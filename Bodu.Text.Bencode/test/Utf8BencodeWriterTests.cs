// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter" /> emits canonical Bencode bytes for every value kind and structure,
/// sorts dictionary keys into ascending bytewise order, and reports lifecycle misuse.
/// </summary>
[TestClass]
public class Utf8BencodeWriterTests
{
    /// <summary>
    /// Encapsulates a program that drives a <see cref="Utf8BencodeWriter" />, expressed as a named delegate because
    /// the writer is a <see langword="ref struct" /> and therefore cannot be a generic type argument such as
    /// <see cref="Action{T}" />.
    /// </summary>
    /// <param name="writer">The writer to drive.</param>
    private delegate void WriterAction(Utf8BencodeWriter writer);

    /// <summary>
    /// Writes a payload through a fresh writer and returns the emitted bytes decoded as Latin-1 text.
    /// </summary>
    /// <param name="write">The callback that drives the writer.</param>
    /// <returns>The emitted bytes, decoded as Latin-1.</returns>
    private static string Write(WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        write(writer);
        return Encoding.Latin1.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Verifies that the writer formats integer values, including zero, negatives, and the 64-bit extremes,
    /// canonically.
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    /// <param name="expected">The expected canonical encoding.</param>
    [TestMethod]
    [DataRow(0L, "i0e")]
    [DataRow(1L, "i1e")]
    [DataRow(-1L, "i-1e")]
    [DataRow(42L, "i42e")]
    [DataRow(-12345L, "i-12345e")]
    [DataRow(long.MaxValue, "i9223372036854775807e")]
    [DataRow(long.MinValue, "i-9223372036854775808e")]
    public void WriteInteger_WhenValueGiven_ShouldEmitCanonicalEncoding(long value, string expected)
    {
        var actual = Write(w => w.WriteInteger(value));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the unsigned <see cref="ulong" /> overload formats values canonically across the full unsigned
    /// 64-bit range, including values beyond <see cref="long.MaxValue" /> that the signed overload cannot represent.
    /// </summary>
    /// <param name="value">The unsigned integer value to write.</param>
    /// <param name="expected">The expected canonical encoding.</param>
    [TestMethod]
    [DataRow(0UL, "i0e")]
    [DataRow(42UL, "i42e")]
    [DataRow(9223372036854775807UL, "i9223372036854775807e")]
    [DataRow(9223372036854775808UL, "i9223372036854775808e")]
    [DataRow(ulong.MaxValue, "i18446744073709551615e")]
    public void WriteInteger_WhenUnsignedValueGiven_ShouldEmitCanonicalEncoding(ulong value, string expected)
    {
        var actual = Write(w => w.WriteInteger(value));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an empty byte string is emitted as <c>0:</c>.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenEmpty_ShouldEmitZeroLengthPrefix()
    {
        var actual = Write(w => w.WriteByteString(ReadOnlySpan<byte>.Empty));

        Assert.AreEqual("0:", actual);
    }

    /// <summary>
    /// Verifies that an ASCII byte string is emitted with its byte-count length prefix.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenAscii_ShouldEmitLengthPrefixedContent()
    {
        var actual = Write(w => w.WriteByteString("spam"u8));

        Assert.AreEqual("4:spam", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WriteByteString" /> preserves arbitrary binary content verbatim,
    /// with a length prefix counting bytes.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenBinary_ShouldPreserveBytes()
    {
        byte[] content = [0x00, 0xFF, 0x80, 0x01];
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteByteString(content);

        byte[] expected = [.. Encoding.ASCII.GetBytes("4:"), .. content];
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> encodes text as a UTF-8 byte string whose length
    /// prefix counts bytes rather than characters.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenMultibyteText_ShouldEmitUtf8WithByteLength()
    {
        const string Text = "héllo";
        byte[] content = Encoding.UTF8.GetBytes(Text);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteString(Text);

        byte[] expected = [.. Encoding.ASCII.GetBytes($"{content.Length}:"), .. content];
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> throws <see cref="ArgumentNullException" /> when
    /// the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteString(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WritePropertyName(string)" /> throws
    /// <see cref="ArgumentNullException" /> when the name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNameNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName((string)null!);
        });
    }

    /// <summary>
    /// Verifies that constructing the writer with a <see langword="null" /> output throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOutputNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8BencodeWriter(null!);
        });
    }

    /// <summary>
    /// Verifies that an empty list is emitted as <c>le</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenEmpty_ShouldEmitEmptyList()
    {
        var actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteEndList();
        });

        Assert.AreEqual("le", actual);
    }

    /// <summary>
    /// Verifies that an empty dictionary is emitted as <c>de</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartDictionary_WhenEmpty_ShouldEmitEmptyDictionary()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WriteEndDictionary();
        });

        Assert.AreEqual("de", actual);
    }

    /// <summary>
    /// Verifies that a list preserves element insertion order and mixes value kinds.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenMixedValues_ShouldPreserveOrder()
    {
        var actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteInteger(1);
            w.WriteString("two");
            w.WriteInteger(-3);
            w.WriteEndList();
        });

        Assert.AreEqual("li1e3:twoi-3ee", actual);
    }

    /// <summary>
    /// Verifies that nested lists are emitted with balanced delimiters in insertion order.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenNested_ShouldEmitBalancedDelimiters()
    {
        var actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteStartList();
            w.WriteInteger(1);
            w.WriteEndList();
            w.WriteStartList();
            w.WriteEndList();
            w.WriteEndList();
        });

        Assert.AreEqual("lli1eelee", actual);
    }

    /// <summary>
    /// Verifies that the writer sorts dictionary keys into ascending bytewise order regardless of the order in which
    /// the entries were written.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenKeysOutOfOrder_ShouldSortAscending()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("name");
            w.WriteString("x");
            w.WritePropertyName("Length");
            w.WriteInteger(5);
            w.WriteEndDictionary();
        });

        // 'L' (0x4C) sorts before 'n' (0x6E).
        Assert.AreEqual("d6:Lengthi5e4:name1:xe", actual);
    }

    /// <summary>
    /// Verifies that a shorter key sorts before a longer key that shares its prefix, matching bytewise comparison.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenKeysSharePrefix_ShouldOrderShorterFirst()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("ab");
            w.WriteInteger(2);
            w.WritePropertyName("a");
            w.WriteInteger(1);
            w.WriteEndDictionary();
        });

        Assert.AreEqual("d1:ai1e2:abi2ee", actual);
    }

    /// <summary>
    /// Verifies that binary dictionary keys are sorted by raw byte order.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenBinaryKeys_ShouldSortByByteOrder()
    {
        byte[] high = [0xFF];
        byte[] low = [0x01];
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteStartDictionary();
        writer.WritePropertyName(high);
        writer.WriteInteger(2);
        writer.WritePropertyName(low);
        writer.WriteInteger(1);
        writer.WriteEndDictionary();

        byte[] expected =
        [
            .. Encoding.ASCII.GetBytes("d1:"), 0x01, .. Encoding.ASCII.GetBytes("i1e1:"), 0xFF, .. Encoding.ASCII.GetBytes("i2ee"),
        ];
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that a dictionary nested inside another dictionary is emitted with each level's keys sorted.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenNested_ShouldSortEachLevel()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("b");
            w.WriteStartDictionary();
            w.WritePropertyName("z");
            w.WriteInteger(1);
            w.WritePropertyName("a");
            w.WriteInteger(2);
            w.WriteEndDictionary();
            w.WritePropertyName("a");
            w.WriteInteger(3);
            w.WriteEndDictionary();
        });

        Assert.AreEqual("d1:ai3e1:bd1:ai2e1:zi1eee", actual);
    }

    /// <summary>
    /// Verifies that a torrent-like dictionary holding nested structures and mixed values is emitted in canonical
    /// form.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenTorrentLike_ShouldEmitCanonicalDocument()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("announce");
            w.WriteString("http://tracker");
            w.WritePropertyName("info");
            w.WriteStartDictionary();
            w.WritePropertyName("piece length");
            w.WriteInteger(16384);
            w.WritePropertyName("name");
            w.WriteString("file.txt");
            w.WriteEndDictionary();
            w.WriteEndDictionary();
        });

        Assert.AreEqual(
            "d8:announce14:http://tracker4:infod4:name8:file.txt12:piece lengthi16384eee",
            actual);
    }

    /// <summary>
    /// Verifies that the top level accepts more than one value and emits them in sequence, mirroring the writer's
    /// trust in the caller to produce a single root.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenWrittenTwiceAtTopLevel_ShouldConcatenateValues()
    {
        var actual = Write(w =>
        {
            w.WriteInteger(1);
            w.WriteInteger(2);
        });

        Assert.AreEqual("i1ei2e", actual);
    }

    /// <summary>
    /// Verifies that closing a list when no container is open throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndList_WhenNoContainerOpen_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteEndList();
        });
    }

    /// <summary>
    /// Verifies that closing a dictionary when no container is open throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenNoContainerOpen_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that opening more containers than the configured maximum depth allows throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenExceedingMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 2 });
            writer.WriteStartList();
            writer.WriteStartList();
            writer.WriteStartList();
        });
    }

    /// <summary>
    /// Verifies that opening containers up to exactly the configured maximum depth is permitted.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenAtMaxDepth_ShouldNotThrow()
    {
        var actual = Write(() => new BencodeWriterOptions { MaxDepth = 2 }, w =>
        {
            w.WriteStartList();
            w.WriteStartList();
            w.WriteEndList();
            w.WriteEndList();
        });

        Assert.AreEqual("llee", actual);
    }

    /// <summary>
    /// Writes a payload through a writer configured with the supplied options and returns the emitted Latin-1 text.
    /// </summary>
    /// <param name="optionsFactory">The factory producing the writer options.</param>
    /// <param name="write">The callback that drives the writer.</param>
    /// <returns>The emitted bytes, decoded as Latin-1.</returns>
    private static string Write(Func<BencodeWriterOptions> optionsFactory, WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, optionsFactory());
        write(writer);
        return Encoding.Latin1.GetString(buffer.WrittenSpan);
    }
}

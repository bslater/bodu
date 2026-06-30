// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteEndDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteEndDictionary" /> closes a dictionary container.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that closing a dictionary containing two entries with the same key throws
    /// <see cref="BencodeSerializationException" />, because canonical Bencode (BEP 3) forbids duplicate keys.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeys_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that a duplicate-key failure on a root dictionary leaves the destination untouched: validation must
    /// complete before any byte of the dictionary is emitted, so a failed close does not leak a partial document.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeysAtRoot_ShouldEmitNothing()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("b");
            writer.WriteInteger(2);
            writer.WritePropertyName("b");
            writer.WriteInteger(3);
            writer.WriteEndDictionary();
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that closing a dictionary whose duplicate keys are separated by another entry throws
    /// <see cref="BencodeSerializationException" />, confirming duplicates are detected after the canonical sort
    /// rather than only between adjacent writes.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeysNotAdjacent_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("b");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WritePropertyName("b");
            writer.WriteInteger(3);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that closing a dictionary containing two entries with the same non-UTF-8 binary key throws
    /// <see cref="BencodeSerializationException" />, confirming duplicate detection compares raw key bytes.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateBinaryKeys_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();
        byte[] key = [0xFF, 0x00, 0x80];

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName(key);
            writer.WriteInteger(1);
            writer.WritePropertyName(key);
            writer.WriteInteger(2);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that closing a dictionary while a property name is still awaiting its value throws
    /// <see cref="InvalidOperationException" /> instead of silently dropping the dangling key.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenPropertyNamePending_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that closing the current container as a dictionary while it is a list throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenCurrentContainerIsList_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartList();
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that the writer sorts dictionary keys into ascending bytewise order regardless of the order in which
    /// the entries were written.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenKeysOutOfOrder_ShouldSortAscending()
    {
        string actual = Write(w =>
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
        string actual = Write(w =>
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
        string actual = Write(w =>
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
        string actual = Write(w =>
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

}

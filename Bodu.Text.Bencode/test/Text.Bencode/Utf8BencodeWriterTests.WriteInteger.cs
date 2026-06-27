// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteInteger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteInteger" /> emits canonical integer tokens.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that writing an integer directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteInteger(1);
        });
    }
    /// <summary>
    /// Verifies that the combined overloads produce the same canonical document as separate property-name and value
    /// calls, across every value kind and both name forms.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenCombinedWithPropertyName_ShouldMatchSeparateCalls()
    {
        string combined = Write(w =>
        {
            w.WriteStartDictionary();
            w.WriteInteger("count", 42);
            w.WriteInteger("size"u8, 7UL);
            w.WriteByteString("blob", [0xFF, 0x00]);
            w.WriteString("name", "file.txt");
            w.WriteStartList("items");
            w.WriteInteger(1);
            w.WriteEndList();
            w.WriteStartDictionary("info"u8);
            w.WriteInteger("length"u8, 9L);
            w.WriteEndDictionary();
            w.WriteEndDictionary();
        });

        string separate = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("count");
            w.WriteInteger(42);
            w.WritePropertyName("size");
            w.WriteInteger(7UL);
            w.WritePropertyName("blob");
            w.WriteByteString([0xFF, 0x00]);
            w.WritePropertyName("name");
            w.WriteString("file.txt");
            w.WritePropertyName("items");
            w.WriteStartList();
            w.WriteInteger(1);
            w.WriteEndList();
            w.WritePropertyName("info");
            w.WriteStartDictionary();
            w.WritePropertyName("length");
            w.WriteInteger(9);
            w.WriteEndDictionary();
            w.WriteEndDictionary();
        });

        Assert.AreEqual(separate, combined);
    }

    /// <summary>
    /// Verifies that a combined overload used outside an open dictionary throws
    /// <see cref="InvalidOperationException" />, inheriting the property-name placement rules.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenCombinedOverloadUsedAtRoot_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteInteger("count", 42);
        });
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
        string actual = Write(w => w.WriteInteger(value));

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
        string actual = Write(w => w.WriteInteger(value));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a second top-level value throws <see cref="InvalidOperationException" />, because a BEP 3
    /// document is a single value and the reader rejects trailing bytes after the root.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenWrittenTwiceAtTopLevel_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteInteger(1);
            writer.WriteInteger(2);
        });
    }

    /// <summary>
    /// Verifies that the top level accepts more than one value and emits them in sequence when
    /// <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is set, the explicit opt-in for concatenated
    /// value framings.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenWrittenTwiceAtTopLevelWithMultipleRootsAllowed_ShouldConcatenateValues()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { AllowMultipleRootValues = true });

        writer.WriteInteger(1);
        writer.WriteInteger(2);

        Assert.AreEqual("i1ei2e", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

}

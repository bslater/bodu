// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.PropertyValuePairs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the combined property-and-value overloads — <see cref="Utf8BencodeWriter.WriteInteger(string, long)" />,
/// <see cref="Utf8BencodeWriter.WriteByteString(string, ReadOnlySpan{byte})" />,
/// <see cref="Utf8BencodeWriter.WriteString(string, string)" />,
/// <see cref="Utf8BencodeWriter.WriteStartList(string)" />, and
/// <see cref="Utf8BencodeWriter.WriteStartDictionary(string)" /> — that mirror the property-writing surface of
/// <see cref="System.Text.Json.Utf8JsonWriter" />, together with the char-span property-name overload.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that the combined overloads produce the same canonical document as separate property-name and value
    /// calls, across every value kind and both name forms.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenCombinedWithPropertyName_ShouldMatchSeparateCalls()
    {
        var combined = Write(w =>
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

        var separate = Write(w =>
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
    /// Verifies that <see cref="Utf8BencodeWriter.WritePropertyName(ReadOnlySpan{char})" /> encodes the characters
    /// as UTF-8, matching the <see langword="string" /> overload byte for byte.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNameIsCharSpan_ShouldEncodeAsUtf8()
    {
        var actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("café".AsSpan());
            w.WriteInteger(1);
            w.WriteEndDictionary();
        });

        var expected = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("café");
            w.WriteInteger(1);
            w.WriteEndDictionary();
        });

        Assert.AreEqual(expected, actual);
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
    /// Verifies that the combined <see cref="Utf8BencodeWriter.WriteString(string, string)" /> overload rejects a
    /// null value with <see cref="ArgumentNullException" /> before any byte is emitted.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenCombinedOverloadValueIsNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteString("name", null!);
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.Options" /> reports the effective configuration, including the
    /// resolved default depth and the root-value policy.
    /// </summary>
    [TestMethod]
    public void Options_WhenWriterConstructed_ShouldReportEffectiveConfiguration()
    {
        var buffer = new ArrayBufferWriter<byte>();

        var defaulted = new Utf8BencodeWriter(buffer);
        Assert.AreEqual(BencodeLimits.AbsoluteMaxDepth, defaulted.Options.MaxDepth);
        Assert.IsFalse(defaulted.Options.AllowMultipleRootValues);

        var configured = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 8, AllowMultipleRootValues = true });
        Assert.AreEqual(8, configured.Options.MaxDepth);
        Assert.IsTrue(configured.Options.AllowMultipleRootValues);
    }
}

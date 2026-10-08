// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> emits UTF-8 string values.
/// </summary>
public partial class Utf8BencodeWriterTests
{
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
    /// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>value</c> when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteString(null!);
        }, "value");
    }

    /// <summary>
    /// Verifies that non-ASCII text written as a value through <see cref="Utf8BencodeWriter.WriteString(string)" />, as a
    /// key through <see cref="Utf8BencodeWriter.WritePropertyName(string)" />, and as a document through
    /// <see cref="BencodeSerializer.Serialize{T}(T, BencodeSerializerOptions?)" /> is prefixed with its UTF-8 byte
    /// length rather than its character count.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueIsNotAscii_ShouldPrefixUtf8ByteLength()
    {
        const string Text = "été";
        const string Emoji = "\U0001F600";
        byte[] expectedValue = [.. "5:"u8, 0xC3, 0xA9, (byte)'t', 0xC3, 0xA9];
        byte[] expectedDictionary = [(byte)'d', .. expectedValue, .. "i1e4:"u8, 0xF0, 0x9F, 0x98, 0x80, .. "i2ee"u8];

        var valueBuffer = new ArrayBufferWriter<byte>();
        var valueWriter = new Utf8BencodeWriter(valueBuffer);
        valueWriter.WriteString(Text);

        var dictionaryBuffer = new ArrayBufferWriter<byte>();
        var dictionaryWriter = new Utf8BencodeWriter(dictionaryBuffer);
        dictionaryWriter.WriteStartDictionary();
        dictionaryWriter.WritePropertyName(Text);
        dictionaryWriter.WriteInteger(1);
        dictionaryWriter.WritePropertyName(Emoji);
        dictionaryWriter.WriteInteger(2);
        dictionaryWriter.WriteEndDictionary();

        CollectionAssert.AreEqual(expectedValue, valueBuffer.WrittenSpan.ToArray());
        CollectionAssert.AreEqual(expectedDictionary, dictionaryBuffer.WrittenSpan.ToArray());
        CollectionAssert.AreEqual(expectedValue, BencodeSerializer.Serialize(Text));
    }
}

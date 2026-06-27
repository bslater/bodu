// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteByteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteByteString" /> emits length-prefixed byte strings.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that writing a byte string directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteByteString("x"u8);
        });
    }

    /// <summary>
    /// Verifies that an empty byte string is emitted as <c>0:</c>.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenEmpty_ShouldEmitZeroLengthPrefix()
    {
        string actual = Write(w => w.WriteByteString(ReadOnlySpan<byte>.Empty));

        Assert.AreEqual("0:", actual);
    }

    /// <summary>
    /// Verifies that an ASCII byte string is emitted with its byte-count length prefix.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenAscii_ShouldEmitLengthPrefixedContent()
    {
        string actual = Write(w => w.WriteByteString("spam"u8));

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

}

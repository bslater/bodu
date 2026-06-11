// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.CopyString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeReader.CopyString(Span{byte})" /> family: lossless raw-byte copies, UTF-8 text
/// decoding into caller-supplied buffers, undersized-destination rejection, and token-kind enforcement.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{byte})" /> copies the token's raw bytes and returns
    /// the byte count, leaving any excess destination capacity untouched.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenDestinationIsBytes_ShouldCopyRawContent()
    {
        var reader = new Utf8BencodeReader(Bytes("4:spam"));
        Assert.IsTrue(reader.Read());
        Span<byte> destination = stackalloc byte[8];

        var written = reader.CopyString(destination);

        Assert.AreEqual(4, written);
        Assert.IsTrue(destination[..written].SequenceEqual("spam"u8));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{byte})" /> preserves binary content that is not
    /// valid UTF-8 byte for byte.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenContentIsBinary_ShouldCopyLosslessly()
    {
        byte[] data = [(byte)'4', (byte)':', 0xFF, 0x00, 0xFE, 0x80];
        var reader = new Utf8BencodeReader(data);
        Assert.IsTrue(reader.Read());
        Span<byte> destination = stackalloc byte[4];

        var written = reader.CopyString(destination);

        ReadOnlySpan<byte> expected = [0xFF, 0x00, 0xFE, 0x80];
        Assert.AreEqual(4, written);
        Assert.IsTrue(destination.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{char})" /> decodes the token as UTF-8 text and
    /// returns the character count.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenDestinationIsChars_ShouldDecodeUtf8Content()
    {
        byte[] data = "5:café"u8.ToArray();
        var reader = new Utf8BencodeReader(data);
        Assert.IsTrue(reader.Read());
        Span<char> destination = stackalloc char[8];

        var written = reader.CopyString(destination);

        Assert.AreEqual(4, written);
        Assert.AreEqual("café", new string(destination[..written]));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{char})" /> decodes invalid UTF-8 sequences to
    /// U+FFFD replacement characters, matching the lossy contract of <see cref="Utf8BencodeReader.GetString" />.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenContentNotValidUtf8_ShouldDecodeWithReplacementCharacters()
    {
        byte[] data = [(byte)'2', (byte)':', 0xFF, 0xFE];
        var reader = new Utf8BencodeReader(data);
        Assert.IsTrue(reader.Read());
        Span<char> destination = stackalloc char[4];

        var written = reader.CopyString(destination);

        Assert.AreEqual("��", new string(destination[..written]));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{byte})" /> throws
    /// <see cref="ArgumentException" /> when the destination is shorter than the token's content.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenByteDestinationTooSmall_ShouldThrowArgumentException()
    {
        var data = Bytes("4:spam");

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            Span<byte> destination = stackalloc byte[3];
            _ = reader.CopyString(destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{char})" /> throws
    /// <see cref="ArgumentException" /> when the destination is shorter than the decoded character count.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenCharDestinationTooSmall_ShouldThrowArgumentException()
    {
        var data = Bytes("4:spam");

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            Span<char> destination = stackalloc char[3];
            _ = reader.CopyString(destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.CopyString(Span{byte})" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void CopyString_WhenOnIntegerToken_ShouldThrowInvalidOperationException()
    {
        var data = Bytes("i42e");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            Span<byte> destination = stackalloc byte[4];
            _ = reader.CopyString(destination);
        });
    }
}

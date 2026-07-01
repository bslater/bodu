// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.TypedAccessors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the typed value accessors of <see cref="Utf8BencodeReader" /> (GetString, GetInt64, GetBytes, and related).
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetString" /> on a byte string carrying non-UTF-8 binary content
    /// decodes with U+FFFD replacement characters rather than throwing, pinning the documented contract that
    /// <see cref="Utf8BencodeReader.GetBytes" /> is the lossless accessor for binary content.
    /// </summary>
    [TestMethod]
    public void GetString_WhenContentNotValidUtf8_ShouldDecodeWithReplacementCharacters()
    {
        // 0xC3 starts a two-byte UTF-8 sequence, but 0x28 ('(') is not a valid continuation byte.
        byte[] bytes = [(byte)'2', (byte)':', 0xC3, 0x28];
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("�(", reader.GetString());
        CollectionAssert.AreEqual(new byte[] { 0xC3, 0x28 }, reader.GetBytes());
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> on a byte-string token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsByteString_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("3:abc");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> on a container-start token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsStartList_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("le");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> before any token is read throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsNone_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i1e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetString" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsInteger_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetString();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetBytes" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenTokenIsInteger_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetBytes();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetString" /> on an end-of-document position throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsNone_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            reader.Read();
            _ = reader.GetString();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetString" /> and <see cref="Utf8BencodeReader.GetBytes" /> both
    /// read a property-name token, since a key is a byte string in key position.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsPropertyName_ShouldReturnKey()
    {
        byte[] bytes = Bytes("d3:cow3:mooe");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());

        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("cow", reader.GetString());
        CollectionAssert.AreEqual(Bytes("cow"), reader.GetBytes());
    }

}

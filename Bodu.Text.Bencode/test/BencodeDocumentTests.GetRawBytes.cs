// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.GetRawBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="BencodeElement.GetRawBytes" />: that every element kind yields its complete encoded form,
/// including the framing tokens, exactly as it appears in the source document.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> on the root element returns the entire source
    /// document byte for byte.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenOnRootElement_ShouldReturnEntireDocument()
    {
        byte[] source = Bytes(TorrentSource);
        using BencodeDocument document = BencodeDocument.Parse(source);

        CollectionAssert.AreEqual(source, document.RootElement.GetRawBytes());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> on a nested dictionary returns the exact encoded
    /// slice including both delimiters — the info-hash scenario the member exists for.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenOnNestedDictionary_ShouldReturnExactEncodedSlice()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));

        byte[] raw = document.RootElement.GetProperty("info").GetRawBytes();

        Assert.AreEqual("d6:lengthi1024e4:name4:teste", Encoding.Latin1.GetString(raw));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> on an integer element includes the <c>i…e</c>
    /// framing.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenOnInteger_ShouldIncludeFraming()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));

        byte[] raw = document.RootElement.GetProperty("info").GetProperty("length").GetRawBytes();

        Assert.AreEqual("i1024e", Encoding.Latin1.GetString(raw));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> on a byte-string element includes the length prefix
    /// and preserves binary content that is not valid UTF-8.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenOnBinaryByteString_ShouldIncludeLengthPrefixAndPreserveBytes()
    {
        byte[] source = [(byte)'l', (byte)'2', (byte)':', 0xFF, 0x00, (byte)'e'];
        using BencodeDocument document = BencodeDocument.Parse(source);

        byte[] raw = document.RootElement[0].GetRawBytes();

        CollectionAssert.AreEqual(new byte[] { (byte)'2', (byte)':', 0xFF, 0x00 }, raw);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> on a list element returns the encoded list with both
    /// delimiters and every child.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenOnList_ShouldReturnEncodedListWithDelimiters()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("d1:ali1ei2ee1:bi3ee"));

        byte[] raw = document.RootElement.GetProperty("a").GetRawBytes();

        Assert.AreEqual("li1ei2ee", Encoding.Latin1.GetString(raw));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetRawBytes" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GetRawBytes_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement root = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.GetRawBytes();
        });
    }
}

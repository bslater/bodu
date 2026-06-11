// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Clone.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="BencodeElement.Clone" />: that clones are independent of the source document's lifetime and
/// preserve the element's content, mirroring <see cref="System.Text.Json.JsonElement.Clone" />.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that a clone remains fully readable after the source document is disposed.
    /// </summary>
    [TestMethod]
    public void Clone_WhenSourceDocumentDisposed_ShouldRemainReadable()
    {
        BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement clone = document.RootElement.GetProperty("info").Clone();
        document.Dispose();

        Assert.AreEqual(BencodeValueKind.Object, clone.ValueKind);
        Assert.AreEqual(1024L, clone.GetProperty("length").GetInt64());
        Assert.AreEqual("test", clone.GetProperty("name").GetString());
    }

    /// <summary>
    /// Verifies that a clone preserves the element's exact encoded bytes.
    /// </summary>
    [TestMethod]
    public void Clone_WhenElementCloned_ShouldPreserveRawBytes()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement info = document.RootElement.GetProperty("info");

        BencodeElement clone = info.Clone();

        CollectionAssert.AreEqual(info.GetRawBytes(), clone.GetRawBytes());
    }

    /// <summary>
    /// Verifies that disposing a clone's backing document is a no-op that leaves the clone readable, matching the
    /// lifetime contract of <see cref="System.Text.Json.JsonElement.Clone" />.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneBackingDocumentDisposed_ShouldRemainReadable()
    {
        BencodeElement clone;
        using (BencodeDocument document = BencodeDocument.Parse(Bytes("li1ei2ee")))
        {
            clone = document.RootElement.Clone();
        }

        // The source document is disposed; the clone's own (non-pooled) document ignores disposal.
        Assert.AreEqual(2, clone.GetArrayLength());
        Assert.AreEqual(1L, clone[0].GetInt64());
        Assert.AreEqual(2L, clone[1].GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.Clone" /> on a disposed document throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Clone_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        BencodeDocument document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement root = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.Clone();
        });
    }
}

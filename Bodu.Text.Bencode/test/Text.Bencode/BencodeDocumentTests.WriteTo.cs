// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="BencodeElement.WriteTo" /> and <see cref="BencodeDocument.WriteTo" />: verbatim re-emission of
/// parsed values through a <see cref="Utf8BencodeWriter" />, both standalone and embedded in a larger document.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeDocument.WriteTo" /> re-emits the parsed document byte for byte.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenWritingWholeDocument_ShouldRoundTripExactBytes()
    {
        var source = Bytes(TorrentSource);
        using var document = BencodeDocument.Parse(source);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        document.WriteTo(writer);

        CollectionAssert.AreEqual(source, buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.WriteTo" /> embeds a parsed element as a dictionary value in a new
    /// document, preserving its exact encoded bytes within the writer's canonical key ordering.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenElementEmbeddedAsDictionaryValue_ShouldEmitExactSlice()
    {
        using var document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement info = document.RootElement.GetProperty("info");
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteStartDictionary();
        writer.WritePropertyName("info");
        info.WriteTo(writer);
        writer.WriteEndDictionary();

        Assert.AreEqual("d4:infod6:lengthi1024e4:name4:testee", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.WriteTo" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes(TorrentSource));
        BencodeElement root = document.RootElement;
        document.Dispose();
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            root.WriteTo(writer);
        });
    }
}

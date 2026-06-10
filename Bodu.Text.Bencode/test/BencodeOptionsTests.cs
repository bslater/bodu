// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="BencodeReaderOptions" />, <see cref="BencodeWriterOptions" />,
/// <see cref="BencodeDocumentOptions" />, and <see cref="BencodeNodeOptions" /> carrier structs and the behaviour they
/// drive in their respective consumers.
/// </summary>
[TestClass]
public class BencodeOptionsTests
{
    /// <summary>
    /// Verifies that a <see cref="Utf8BencodeReader" /> created with a <see cref="BencodeReaderOptions.MaxDepth" /> of
    /// two rejects a document nested deeper than two by throwing <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ReaderOptions_WhenDocumentExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("llleee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes, new BencodeReaderOptions { MaxDepth = 2 });
            while (reader.Read())
            {
                // Drive the reader to completion to surface the nesting-too-deep error.
            }
        });
    }

    /// <summary>
    /// Verifies that a <see cref="Utf8BencodeReader" /> created with a <see cref="BencodeReaderOptions.MaxDepth" /> of
    /// two accepts a document nested within that limit.
    /// </summary>
    [TestMethod]
    public void ReaderOptions_WhenDocumentWithinMaxDepth_ShouldReadToCompletion()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("llee");

        var reader = new Utf8BencodeReader(bytes, new BencodeReaderOptions { MaxDepth = 2 });
        while (reader.Read())
        {
            // The document nests exactly to the limit and must read without error.
        }

        Assert.AreEqual(bytes.Length, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a <see cref="Utf8BencodeWriter" /> created with a <see cref="BencodeWriterOptions.MaxDepth" /> of
    /// one throws <see cref="BencodeSerializationException" /> when a second list is opened beyond that depth.
    /// </summary>
    [TestMethod]
    public void WriterOptions_WhenNestingExceedsMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 1 });
            writer.WriteStartList();
            writer.WriteStartList();
        });
    }

    /// <summary>
    /// Verifies that a <see cref="Utf8BencodeWriter" /> created with a <see cref="BencodeWriterOptions.MaxDepth" /> of
    /// one writes a single list within that limit.
    /// </summary>
    [TestMethod]
    public void WriterOptions_WhenNestingWithinMaxDepth_ShouldWriteCanonicalBytes()
    {
        var buffer = new ArrayBufferWriter<byte>();

        var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 1 });
        writer.WriteStartList();
        writer.WriteInteger(1);
        writer.WriteEndList();

        Assert.AreEqual("li1ee", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeDocument.Parse(ReadOnlySpan{byte}, BencodeDocumentOptions)" /> with a
    /// <see cref="BencodeDocumentOptions.MaxDepth" /> of one throws <see cref="BencodeFormatException" /> on a document
    /// nested two deep.
    /// </summary>
    [TestMethod]
    public void DocumentOptions_WhenDocumentExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("llee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using BencodeDocument document = BencodeDocument.Parse(bytes, new BencodeDocumentOptions { MaxDepth = 1 });
        });
    }

    /// <summary>
    /// Verifies that the default <see cref="BencodeDocument.Parse(byte[])" /> accepts a document the constrained
    /// overload would reject.
    /// </summary>
    [TestMethod]
    public void DocumentOptions_WhenDefaultDepth_ShouldParseNestedDocument()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("llee");

        using BencodeDocument document = BencodeDocument.Parse(bytes);

        Assert.AreEqual(BencodeValueKind.Array, document.RootElement.ValueKind);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.Parse(ReadOnlySpan{byte}, BencodeNodeOptions)" /> with
    /// <see cref="BencodeNodeOptions.PropertyNameCaseInsensitive" /> enabled resolves a property under differently
    /// cased names.
    /// </summary>
    [TestMethod]
    public void NodeOptions_WhenCaseInsensitive_ShouldResolvePropertyIgnoringCase()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name4:teste");

        BencodeObject obj = BencodeNode.Parse(bytes, new BencodeNodeOptions { PropertyNameCaseInsensitive = true })!.AsObject();

        Assert.IsTrue(obj.ContainsKey("name"));
        Assert.IsTrue(obj.ContainsKey("NAME"));
        Assert.AreEqual("test", (string)obj["name"]!);
        Assert.AreEqual("test", (string)obj["NAME"]!);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.Parse(ReadOnlySpan{byte})" /> with default options performs case-sensitive
    /// property lookups, matching the serializer's behaviour.
    /// </summary>
    [TestMethod]
    public void NodeOptions_WhenDefault_ShouldResolvePropertyCaseSensitively()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name4:teste");

        BencodeObject obj = BencodeNode.Parse(bytes)!.AsObject();

        Assert.IsFalse(obj.ContainsKey("name"));
        Assert.IsTrue(obj.ContainsKey("Name"));
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeObject" /> built with
    /// <see cref="BencodeNodeOptions.PropertyNameCaseInsensitive" /> enabled looks up keys case-insensitively.
    /// </summary>
    [TestMethod]
    public void NodeOptions_WhenObjectBuiltCaseInsensitive_ShouldLookUpKeysIgnoringCase()
    {
        var obj = new BencodeObject(new BencodeNodeOptions { PropertyNameCaseInsensitive = true })
        {
            ["Name"] = "test",
        };

        Assert.IsTrue(obj.ContainsKey("name"));
        Assert.IsTrue(obj.TryGetValue("NAME", out BencodeNode? value));
        Assert.AreEqual("test", (string)value!);
    }
}

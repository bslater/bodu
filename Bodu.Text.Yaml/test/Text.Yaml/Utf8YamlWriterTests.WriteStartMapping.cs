// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WriteStartMapping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WriteStartMapping" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>Verifies that a simple mapping of scalars is emitted in block style.</summary>
    [TestMethod]
    public void WriteStartMapping_WhenMappingOfScalars_ShouldEmitBlock()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("a");
            w.WriteInt64(1);
            w.WritePropertyName("b");
            w.WriteString("two");
            w.WriteEndMapping();
        });

        Assert.AreEqual("a: 1\nb: two\n", yaml);
    }

    /// <summary>Verifies that a nested mapping is indented under its key.</summary>
    [TestMethod]
    public void WriteStartMapping_WhenNested_ShouldIndent()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("server");
            w.WriteStartMapping();
            w.WritePropertyName("host");
            w.WriteString("localhost");
            w.WriteEndMapping();
            w.WriteEndMapping();
        });

        Assert.AreEqual("server:\n  host: localhost\n", yaml);
    }

    /// <summary>Verifies that empty collections are emitted in flow form so they round-trip as empty.</summary>
    [TestMethod]
    public void WriteStartMapping_WhenEmptyCollections_ShouldEmitFlowEmpty()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("list");
            w.WriteStartSequence();
            w.WriteEndSequence();
            w.WritePropertyName("map");
            w.WriteStartMapping();
            w.WriteEndMapping();
            w.WriteEndMapping();
        });

        Assert.AreEqual("list: []\nmap: {}\n", yaml);
    }

    /// <summary>Verifies that a written document round-trips back through the reader.</summary>
    [TestMethod]
    public void WriteStartMapping_WhenComplexDocument_ShouldRoundTrip()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("name");
            w.WriteString("test");
            w.WritePropertyName("servers");
            w.WriteStartSequence();
            w.WriteStartMapping();
            w.WritePropertyName("host");
            w.WriteString("a");
            w.WritePropertyName("port");
            w.WriteInt64(80);
            w.WriteEndMapping();
            w.WriteEndSequence();
            w.WriteEndMapping();
        });

        using var doc = YamlDocument.Parse(yaml);
        Assert.AreEqual("test", doc.RootElement.GetProperty("name").GetString());
        var servers = doc.RootElement.GetProperty("servers");
        Assert.AreEqual(1, servers.GetSequenceLength());
        Assert.AreEqual("a", servers[0].GetProperty("host").GetString());
        Assert.AreEqual(80L, servers[0].GetProperty("port").GetInt64());
    }

    /// <summary>
    /// Verifies that exceeding the configured writer depth throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartMapping_WhenExceedingMaxDepth_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer, new YamlWriterOptions { MaxDepth = 2 });
            writer.WriteStartMapping();
            writer.WritePropertyName("a");
            writer.WriteStartMapping();
            writer.WritePropertyName("b");
            writer.WriteStartMapping();
        });
    }

    /// <summary>
    /// Verifies that the configured newline sequence is used between lines.
    /// </summary>
    [TestMethod]
    public void WriteStartMapping_WhenCarriageReturnNewLine_ShouldUseConfiguredNewLine()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer, new YamlWriterOptions { NewLine = "\r\n" });
        writer.WriteStartMapping();
        writer.WritePropertyName("a");
        writer.WriteInt64(1);
        writer.WriteEndMapping();

        Assert.AreEqual("a: 1\r\n", System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

}

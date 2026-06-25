// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the block-style emission of <see cref="Utf8YamlWriter" />.
/// </summary>
[TestClass]
public partial class Utf8YamlWriterTests
{
    /// <summary>Writes via the supplied callback and returns the produced UTF-8 text.</summary>
    /// <param name="write">The emission callback.</param>
    /// <returns>The written YAML text.</returns>
    private static string Write(WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);
        write(ref writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>A by-ref writer callback used to drive emission within a test.</summary>
    /// <param name="writer">The writer to emit into.</param>
    private delegate void WriterAction(ref Utf8YamlWriter writer);

    /// <summary>Verifies that a simple mapping of scalars is emitted in block style.</summary>
    [TestMethod]
    public void Write_WhenMappingOfScalars_ShouldEmitBlock()
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
    public void Write_WhenNestedMapping_ShouldIndent()
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

    /// <summary>Verifies that a sequence value is emitted with dash entries.</summary>
    [TestMethod]
    public void Write_WhenSequence_ShouldEmitDashes()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("items");
            w.WriteStartSequence();
            w.WriteString("a");
            w.WriteInt64(2);
            w.WriteEndSequence();
            w.WriteEndMapping();
        });

        Assert.AreEqual("items:\n  - a\n  - 2\n", yaml);
    }

    /// <summary>Verifies that empty collections are emitted in flow form so they round-trip as empty.</summary>
    [TestMethod]
    public void Write_WhenEmptyCollections_ShouldEmitFlowEmpty()
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

    /// <summary>Verifies that a string which would resolve to another type is quoted.</summary>
    [TestMethod]
    public void Write_WhenAmbiguousString_ShouldQuote()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("a");
            w.WriteString("123");
            w.WritePropertyName("b");
            w.WriteString("true");
            w.WritePropertyName("c");
            w.WriteString("no");
            w.WriteEndMapping();
        });

        Assert.AreEqual("a: \"123\"\nb: \"true\"\nc: \"no\"\n", yaml);
    }

    /// <summary>Verifies that a value containing special characters is double-quoted with escapes.</summary>
    [TestMethod]
    public void Write_WhenSpecialCharacters_ShouldEscape()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("text");
            w.WriteString("line1\nline2\ttab");
            w.WriteEndMapping();
        });

        Assert.AreEqual("text: \"line1\\nline2\\ttab\"\n", yaml);
    }

    /// <summary>Verifies that a written document round-trips back through the reader.</summary>
    [TestMethod]
    public void Write_WhenComplexDocument_ShouldRoundTrip()
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
}

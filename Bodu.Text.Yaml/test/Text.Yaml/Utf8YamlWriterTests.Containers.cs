// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.Containers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies container emission of <see cref="Utf8YamlWriter" />: nested mappings, sequences, empty collections, and
/// round-tripping a complex document back through the parser.
/// </summary>
public partial class Utf8YamlWriterTests
{
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

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the core structural parsing of block mappings, sequences, and document markers into the read-only
/// <see cref="YamlDocument" /> object model.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a nested block mapping is parsed into nested mapping nodes.</summary>
    [TestMethod]
    public void Parse_WhenNestedMapping_ShouldNest()
    {
        using var doc = YamlDocument.Parse("server:\n  host: localhost\n  port: 8080\n");
        var server = doc.RootElement.GetProperty("server");

        Assert.AreEqual(YamlValueKind.Mapping, server.ValueKind);
        Assert.AreEqual("localhost", server.GetProperty("host").GetString());
        Assert.AreEqual(8080L, server.GetProperty("port").GetInt64());
    }

    /// <summary>Verifies that a block sequence is parsed into a sequence of scalars.</summary>
    [TestMethod]
    public void Parse_WhenBlockSequence_ShouldYieldElements()
    {
        using var doc = YamlDocument.Parse("- one\n- two\n- 3\n");
        var root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Sequence, root.ValueKind);
        Assert.AreEqual(3, root.GetSequenceLength());
        Assert.AreEqual("one", root[0].GetString());
        Assert.AreEqual("two", root[1].GetString());
        Assert.AreEqual(3L, root[2].GetInt64());
    }

    /// <summary>Verifies that a mapping whose value is a block sequence indented at the key column is parsed.</summary>
    [TestMethod]
    public void Parse_WhenMappingOfSequence_ShouldParse()
    {
        using var doc = YamlDocument.Parse("items:\n  - a\n  - b\nname: list\n");
        var root = doc.RootElement;

        var items = root.GetProperty("items");
        Assert.AreEqual(YamlValueKind.Sequence, items.ValueKind);
        Assert.AreEqual(2, items.GetSequenceLength());
        Assert.AreEqual("a", items[0].GetString());
        Assert.AreEqual("list", root.GetProperty("name").GetString());
    }

    /// <summary>Verifies that a sequence of mappings is parsed correctly.</summary>
    [TestMethod]
    public void Parse_WhenSequenceOfMappings_ShouldParse()
    {
        using var doc = YamlDocument.Parse("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        var root = doc.RootElement;

        Assert.AreEqual(2, root.GetSequenceLength());
        Assert.AreEqual("a", root[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, root[1].GetProperty("age").GetInt64());
    }

    /// <summary>Verifies that a leading document-start marker is accepted.</summary>
    [TestMethod]
    public void Parse_WhenDocumentStartMarker_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("---\nkey: value\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that a mapping value that is a block sequence at the key column parses.</summary>
    [TestMethod]
    public void Parse_WhenMixedMappingSequence_ShouldParse()
    {
        using var d = Doc("key1: value1\nkey2:\n  - item1\n  - item2\n");
        Assert.AreEqual("value1", d.RootElement.GetProperty("key1").GetString());
        Assert.AreEqual(2, d.RootElement.GetProperty("key2").GetSequenceLength());
        Assert.AreEqual("item1", d.RootElement.GetProperty("key2")[0].GetString());
    }

    /// <summary>Verifies that a sequence of mappings with multiple keys per entry parses.</summary>
    [TestMethod]
    public void Parse_WhenSequenceOfMultiKeyMappings_ShouldParse()
    {
        using var d = Doc("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        Assert.AreEqual("a", d.RootElement[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, d.RootElement[1].GetProperty("age").GetInt64());
    }
}

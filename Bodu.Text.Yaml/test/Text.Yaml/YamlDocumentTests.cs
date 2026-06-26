// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies parsing of YAML source into the read-only <see cref="YamlDocument" /> object model. Scalar typing,
/// quoting, block scalars, flow collections, anchors, merge keys, tags, multi-document streams, and profile
/// enforcement are covered in the subject-specific partial files.
/// </summary>
[TestClass]
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a simple block mapping resolves scalar value kinds correctly.</summary>
    [TestMethod]
    public void Parse_WhenBlockMapping_ShouldResolveScalarKinds()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: two\nc: true\nd: 3.5\ne: null\nf: ~");
        var root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Mapping, root.ValueKind);
        Assert.AreEqual(1L, root.GetProperty("a").GetInt64());
        Assert.AreEqual("two", root.GetProperty("b").GetString());
        Assert.IsTrue(root.GetProperty("c").GetBoolean());
        Assert.AreEqual(3.5, root.GetProperty("d").GetDouble());
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("e").ValueKind);
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("f").ValueKind);
    }

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

    /// <summary>Verifies that comments and blank lines are ignored.</summary>
    [TestMethod]
    public void Parse_WhenCommentsAndBlankLines_ShouldIgnore()
    {
        using var doc = YamlDocument.Parse("# header\n\na: 1   # inline\n\n# trailing\nb: 2\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that a leading document-start marker is accepted.</summary>
    [TestMethod]
    public void Parse_WhenDocumentStartMarker_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("---\nkey: value\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that mapping enumeration yields all pairs in order.</summary>
    [TestMethod]
    public void EnumerateMapping_ShouldYieldPairsInOrder()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: 2\nc: 3\n");
        var keys = new List<string>();
        foreach (var pair in doc.RootElement.EnumerateMapping())
            keys.Add(pair.Name);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, keys);
    }
}

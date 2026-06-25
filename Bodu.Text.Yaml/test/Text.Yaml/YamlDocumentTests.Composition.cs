// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Composition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies composition behaviour: anchors, aliases, merge keys, tags, and multi-document streams.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that an alias resolves to the value of its anchor.</summary>
    [TestMethod]
    public void Parse_WhenAlias_ShouldResolveToAnchorValue()
    {
        using var doc = YamlDocument.Parse("a: &id 42\nb: *id\n");
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(42L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that an alias to an anchored mapping resolves to the whole mapping.</summary>
    [TestMethod]
    public void Parse_WhenAliasToMapping_ShouldResolveSubtree()
    {
        using var doc = YamlDocument.Parse("base: &b\n  x: 1\n  y: 2\nuse: *b\n");
        var use = doc.RootElement.GetProperty("use");
        Assert.AreEqual(YamlValueKind.Mapping, use.ValueKind);
        Assert.AreEqual(1L, use.GetProperty("x").GetInt64());
        Assert.AreEqual(2L, use.GetProperty("y").GetInt64());
    }

    /// <summary>Verifies that a merge key imports keys from the merged mapping without overriding explicit keys.</summary>
    [TestMethod]
    public void Parse_WhenMergeKey_ShouldImportWithoutOverriding()
    {
        using var doc = YamlDocument.Parse(
            "defaults: &d\n  size: medium\n  color: red\nitem:\n  <<: *d\n  color: blue\n");
        var item = doc.RootElement.GetProperty("item");
        Assert.AreEqual("medium", item.GetProperty("size").GetString());
        Assert.AreEqual("blue", item.GetProperty("color").GetString());
        Assert.IsFalse(item.TryGetProperty("<<", out _));
    }

    /// <summary>Verifies that a merge key accepting a sequence of mappings applies earlier-source precedence.</summary>
    [TestMethod]
    public void Parse_WhenMergeSequence_ShouldApplyPrecedence()
    {
        using var doc = YamlDocument.Parse(
            "a: &a {x: 1, y: 1}\nb: &b {y: 2, z: 2}\nc:\n  <<: [*a, *b]\n");
        var c = doc.RootElement.GetProperty("c");
        Assert.AreEqual(1L, c.GetProperty("x").GetInt64());
        Assert.AreEqual(1L, c.GetProperty("y").GetInt64()); // first source wins
        Assert.AreEqual(2L, c.GetProperty("z").GetInt64());
    }

    /// <summary>Verifies that the <c>!!str</c> tag forces a numeric-looking scalar to remain a string.</summary>
    [TestMethod]
    public void Parse_WhenStrTag_ShouldForceString()
    {
        using var doc = YamlDocument.Parse("a: !!str 123\nb: !!str true\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual("true", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that the <c>!!int</c> tag reinterprets a quoted scalar as an integer.</summary>
    [TestMethod]
    public void Parse_WhenIntTag_ShouldForceInteger()
    {
        using var doc = YamlDocument.Parse("a: !!int \"42\"\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>Verifies that an undefined alias is rejected.</summary>
    [TestMethod]
    public void Parse_WhenUndefinedAlias_ShouldThrow()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("a: *missing\n");
        });

        Assert.IsTrue(ex.Message.Contains("missing", StringComparison.Ordinal));
    }

    /// <summary>Verifies that a multi-document stream yields each document in order.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenMultipleDocuments_ShouldYieldEach()
    {
        var docs = YamlDocument.ParseAllDocuments("---\na: 1\n---\nb: 2\n...\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual(1L, docs[0].RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, docs[1].RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that a single-document source still yields one document through the stream API.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenSingleDocument_ShouldYieldOne()
    {
        var docs = YamlDocument.ParseAllDocuments("key: value\n");
        Assert.AreEqual(1, docs.Count);
        Assert.AreEqual("value", docs[0].RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that nesting beyond the configured maximum depth is rejected.</summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrow()
    {
        var deep = string.Concat(Enumerable.Repeat("[", 200)) + "x" + string.Concat(Enumerable.Repeat("]", 200));
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("root: " + deep + "\n");
        });
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Anchors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies anchor and alias composition: an alias resolves to its anchor's value or subtree, and an undefined
/// alias is rejected.
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

    /// <summary>Verifies that an anchored mapping aliased elsewhere resolves to the same content.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenAnchoredMappingAliased_ShouldResolveContent()
    {
        using var doc = YamlDocument.Parse("defaults: &x\n  a: 1\n  b: 2\nconfig: *x\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("config").GetProperty("b").GetInt64());
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
}

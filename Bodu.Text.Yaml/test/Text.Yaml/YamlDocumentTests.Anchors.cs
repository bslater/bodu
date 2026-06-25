// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Anchors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies anchor and alias resolution: scalar aliases, mapping subtrees, and rejection of undefined aliases.
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
    public void Parse_WhenAnchoredMappingAliased_ShouldResolveSameContent()
    {
        using var d = Doc("defaults: &x\n  a: 1\n  b: 2\nconfig: *x\n");
        Assert.AreEqual(1L, d.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, d.RootElement.GetProperty("config").GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that an undefined alias is rejected with a message naming the missing anchor.</summary>
    [TestMethod]
    public void Parse_WhenUndefinedAlias_ShouldThrow()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("a: *missing\n");
        });

        Assert.IsTrue(ex.Message.Contains("missing", StringComparison.Ordinal));
    }

    /// <summary>Verifies that an alias to an undefined anchor inside a mapping value is rejected.</summary>
    [TestMethod]
    public void Parse_WhenUndefinedAliasInMappingValue_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var d = Doc("config: *undefined\n");
        });
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MergeKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies merge-key (<c>&lt;&lt;</c>) semantics: import without override, explicit-key override, and the
/// earlier-source precedence rule for a sequence of merged mappings.
/// </summary>
public partial class YamlDocumentTests
{
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

    /// <summary>Verifies that a merge key inserts defaults and an explicit key overrides them.</summary>
    [TestMethod]
    public void Parse_WhenMergeKeyOverridden_ShouldPreferExplicitKey()
    {
        using var d = Doc("defaults: &x\n  a: 1\n  b: 2\nconfig:\n  <<: *x\n  a: 999\n");
        Assert.AreEqual(999L, d.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, d.RootElement.GetProperty("config").GetProperty("b").GetInt64());
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
}

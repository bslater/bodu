// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDirectiveAndAliasTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the parser's handling of directives, tag-handle expansion, the merge key, and alias cycles under the
/// library's YAML profile.
/// </summary>
[TestClass]
public sealed class YamlDirectiveAndAliasTests
{
    /// <summary>
    /// Verifies that a valid <c>%YAML 1.2</c> directive is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenYamlDirectiveIsValid_ShouldAccept()
    {
        using var doc = YamlDocument.Parse("%YAML 1.2\n---\na: 1\n");

        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>
    /// Verifies that an unsupported YAML version directive is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenYamlDirectiveVersionUnsupported_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("%YAML 1.3\n---\na: 1\n");
        });
    }

    /// <summary>
    /// Verifies that a repeated <c>%YAML</c> directive is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenYamlDirectiveRepeated_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("%YAML 1.2\n%YAML 1.2\n---\na: 1\n");
        });
    }

    /// <summary>
    /// Verifies that a repeated <c>%TAG</c> handle is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTagHandleRepeated_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("%TAG !! tag:a,2000:\n%TAG !! tag:b,2000:\n---\nv: 1\n");
        });
    }

    /// <summary>
    /// Verifies that the core <c>!!str</c> tag forces a scalar to a string when no overriding handle is in effect.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCoreStrTag_ShouldForceString()
    {
        using var doc = YamlDocument.Parse("!!str 123\n");

        Assert.AreEqual(YamlValueKind.String, doc.RootElement.ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetString());
    }

    /// <summary>
    /// Verifies that redefining the secondary handle with <c>%TAG</c> makes <c>!!str</c> resolve to a non-core tag,
    /// which the profile rejects as an unsupported tag.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSecondaryHandleRedefined_ShouldRejectUnknownTag()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("%TAG !! tag:example.com,2000:\n---\n!!str 123\n");
        });
    }

    /// <summary>
    /// Verifies that a merge key expands the referenced mapping's keys by default, with the local key taking
    /// precedence.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMergeKeyDefault_ShouldExpand()
    {
        using var doc = YamlDocument.Parse("base: &b\n  a: 1\n  c: 3\nobj:\n  <<: *b\n  a: 2\n");

        var obj = doc.RootElement.GetProperty("obj");
        Assert.AreEqual(2L, obj.GetProperty("a").GetInt64());
        Assert.AreEqual(3L, obj.GetProperty("c").GetInt64());
    }

    /// <summary>
    /// Verifies that disabling merge-key handling retains <c>&lt;&lt;</c> as an ordinary key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMergeKeyDisabled_ShouldRetainLiteralKey()
    {
        using var doc = YamlDocument.Parse(
            Encoding.UTF8.GetBytes("base: &b\n  a: 1\nobj:\n  <<: *b\n  z: 9\n"),
            new YamlDocumentOptions { MergeKeyBehavior = YamlMergeKeyBehavior.Disabled });

        var obj = doc.RootElement.GetProperty("obj");
        Assert.IsTrue(obj.TryGetProperty("<<", out _));
        Assert.IsFalse(obj.TryGetProperty("a", out _));
    }

    /// <summary>
    /// Verifies that an alias which forms a cycle is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAliasFormsCycle_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: &a [*a]\n");
        });
    }
}

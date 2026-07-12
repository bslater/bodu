// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the token stream produced by <see cref="Utf8YamlReader.Read" />.
/// </summary>
public partial class Utf8YamlReaderTests
{
    /// <summary>Verifies that a block mapping produces start, property-name/value, and end tokens in order.</summary>
    [TestMethod]
    public void Read_WhenBlockMapping_ShouldEmitTokensInOrder()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("a: 1\nb: hi\n"));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                YamlTokenType.String => $"str:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new[] { "StartMapping", "key:a", "int:1", "key:b", "str:hi", "EndMapping" },
            tokens);
    }

    /// <summary>Verifies that a nested sequence within a mapping emits correctly ordered structural tokens.</summary>
    [TestMethod]
    public void Read_WhenNestedSequence_ShouldEmitNestedTokens()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("items: [1, 2]\n"));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new[] { "StartMapping", "key:items", "StartSequence", "int:1", "int:2", "EndSequence", "EndMapping" },
            tokens);
    }

    /// <summary>Verifies that a scalar document root emits a single scalar token.</summary>
    [TestMethod]
    public void Read_WhenScalarRoot_ShouldEmitSingleToken()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("42"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(YamlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(42L, reader.GetInt64());
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an alias referencing an anchored scalar is resolved transparently, so the token stream repeats the
    /// anchored value with no alias-specific token — pinning that alias resolution happens below the reader surface.
    /// </summary>
    [TestMethod]
    public void Read_WhenAliasReferencesAnchoredScalar_ShouldEmitResolvedValue()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("a: &x 5\nb: *x\n"));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new[] { "StartMapping", "key:a", "int:5", "key:b", "int:5", "EndMapping" },
            tokens);
    }

    /// <summary>
    /// Verifies that an alias referencing an anchored mapping re-emits the whole anchored subtree at the alias site, so
    /// a reader-driven binder observes the same structure the document object model resolves.
    /// </summary>
    [TestMethod]
    public void Read_WhenAliasReferencesAnchoredMapping_ShouldReEmitSubtree()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("base: &b {x: 1}\ncopy: *b\n"));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new[]
            {
                "StartMapping",
                "key:base", "StartMapping", "key:x", "int:1", "EndMapping",
                "key:copy", "StartMapping", "key:x", "int:1", "EndMapping",
                "EndMapping",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a merge key (<c>&lt;&lt;</c>) is expanded before tokens are emitted, so the merged properties
    /// appear as ordinary property-name/value pairs alongside the mapping's explicit keys.
    /// </summary>
    [TestMethod]
    public void Read_WhenMergeKeyDefault_ShouldEmitExpandedProperties()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("base: &b\n  x: 1\nchild:\n  <<: *b\n  y: 2\n"));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                _ => reader.TokenType.ToString(),
            });
        }

        Assert.IsTrue(tokens.Contains("key:y"), "explicit key should be present");
        Assert.IsTrue(tokens.Count(static t => t == "key:x") == 2, "merged key should appear in both mappings");
        Assert.IsFalse(tokens.Any(static t => t.Contains("<<", StringComparison.Ordinal)), "merge key must not surface as a token");
    }

    /// <summary>
    /// Verifies that the duplicate-key policy is applied before tokens are emitted: with
    /// <see cref="YamlDuplicateKeyBehavior.UseLast" /> a duplicated key surfaces once, carrying the last value.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeyWithUseLast_ShouldEmitLastValueOnce()
    {
        var options = new YamlReaderOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseLast };
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("a: 1\na: 2\n"), options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                YamlTokenType.PropertyName => $"key:{reader.GetString()}",
                YamlTokenType.Integer => $"int:{reader.GetInt64()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new[] { "StartMapping", "key:a", "int:2", "EndMapping" },
            tokens);
    }
}

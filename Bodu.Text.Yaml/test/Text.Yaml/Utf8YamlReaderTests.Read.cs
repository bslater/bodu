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
}

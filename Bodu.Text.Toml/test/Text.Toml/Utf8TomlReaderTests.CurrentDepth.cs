// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.CurrentDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that the bracket depth follows the <see cref="System.Text.Json.Utf8JsonReader.CurrentDepth" />
    /// convention: a container's start and end tokens report the depth of the enclosing context, and elements report
    /// the container's depth.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_WhenNestedContainers_ShouldFollowUtf8JsonReaderConvention()
    {
        Utf8TomlReader reader = Create("v = [[1], { x = 2 }]\n");

        Advance(ref reader, 1);
        Assert.AreEqual(0, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.StartArray, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.StartArray, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(2, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.EndArray, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.StartInlineTable, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Advance(ref reader, 2);
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(2, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.EndInlineTable, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.EndArray, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
    }

    /// <summary>
    /// Verifies that table headers do not contribute to the bracket depth: header nesting is structural, not lexical.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_WhenTableHeaders_ShouldRemainZero()
    {
        Utf8TomlReader reader = Create("[a.b.c]\nx = 1\n");

        while (reader.Read())
            Assert.AreEqual(0, reader.CurrentDepth);
    }
}

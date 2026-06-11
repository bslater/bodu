// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Skip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that skipping on a container start advances to the matching end token in source order.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnStartArray_ShouldAdvanceToMatchingEnd()
    {
        Utf8TomlReader reader = Create("v = [1, [2, 3], { x = 4 }]\nnext = 5\n");

        Advance(ref reader, 2);
        Assert.AreEqual(TomlTokenType.StartArray, reader.TokenType);

        reader.Skip();
        Assert.AreEqual(TomlTokenType.EndArray, reader.TokenType);

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.Key, reader.TokenType);
        Assert.IsTrue(reader.ValueTextEquals("next"u8));
    }

    /// <summary>
    /// Verifies that skipping on a key advances over the remaining segments and the value, finishing on the value's
    /// last token.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnKey_ShouldAdvancePastDottedSegmentsAndValue()
    {
        Utf8TomlReader reader = Create("a.b.c = { x = 1 }\nnext = 2\n");

        Advance(ref reader, 1);
        Assert.AreEqual(TomlTokenType.Key, reader.TokenType);

        reader.Skip();
        Assert.AreEqual(TomlTokenType.EndInlineTable, reader.TokenType);

        Advance(ref reader, 1);
        Assert.IsTrue(reader.ValueTextEquals("next"u8));
    }

    /// <summary>
    /// Verifies that skipping on a scalar token has no effect.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnScalar_ShouldHaveNoEffect()
    {
        Utf8TomlReader reader = Create("a = 1\n");

        Advance(ref reader, 2);
        reader.Skip();
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TrySkip" /> skips like <see cref="Utf8TomlReader.Skip" /> and reports
    /// success, since the reader always holds the final block.
    /// </summary>
    [TestMethod]
    public void TrySkip_WhenOnStartInlineTable_ShouldSkipAndReturnTrue()
    {
        Utf8TomlReader reader = Create("p = { x = 1, y = 2 }\n");

        Advance(ref reader, 2);
        Assert.IsTrue(reader.TrySkip());
        Assert.AreEqual(TomlTokenType.EndInlineTable, reader.TokenType);
    }
}

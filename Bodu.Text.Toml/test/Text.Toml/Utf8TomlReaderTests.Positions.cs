// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Positions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that token positions are reported as byte offsets, lines, and byte columns for ASCII input.
    /// </summary>
    [TestMethod]
    public void TokenStartIndex_WhenAsciiDocument_ShouldReportBytePositions()
    {
        Utf8TomlReader lexer = Create("a = 1\nbb = 2\n");

        Advance(ref lexer, 1);
        Assert.AreEqual(0, lexer.TokenStartIndex);
        Assert.AreEqual(1, lexer.LineNumber);
        Assert.AreEqual(1, lexer.ColumnNumber);

        Advance(ref lexer, 1);
        Assert.AreEqual(4, lexer.TokenStartIndex);
        Assert.AreEqual(5, lexer.ColumnNumber);

        Advance(ref lexer, 1);
        Assert.AreEqual(6, lexer.TokenStartIndex);
        Assert.AreEqual(2, lexer.LineNumber);
        Assert.AreEqual(1, lexer.ColumnNumber);
    }

    /// <summary>
    /// Verifies that positions after a multi-byte UTF-8 character are byte-true: the column and offset count bytes,
    /// not decoded characters.
    /// </summary>
    [TestMethod]
    public void TokenStartIndex_WhenMultiByteContent_ShouldCountBytes()
    {
        // The first line is s(0) sp(1) =(2) sp(3) "(4) é(5,6) "(7) LF(8); the second key starts at byte 9.
        Utf8TomlReader lexer = Create("s = \"é\"\nx = 1\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(4, lexer.TokenStartIndex);
        Assert.AreEqual(5, lexer.ColumnNumber);
        Assert.AreEqual(2, lexer.ValueSpan.Length);

        Advance(ref lexer, 1);
        Assert.AreEqual(TomlTokenType.Key, lexer.TokenType);
        Assert.AreEqual(9, lexer.TokenStartIndex);
        Assert.AreEqual(2, lexer.LineNumber);
        Assert.AreEqual(1, lexer.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a lexical error carries the exact byte-true line, column, and offset of the failure point.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedString_ShouldReportExactBytePosition()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            Utf8TomlReader lexer = Create("a = \"x");
            Drain(ref lexer);
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(7, ex.ColumnNumber);
        Assert.AreEqual(6, ex.Offset);
    }

    /// <summary>
    /// Verifies that an error on a later line reports that line, with the column counted from the line start.
    /// </summary>
    [TestMethod]
    public void Read_WhenErrorOnSecondLine_ShouldReportSecondLinePosition()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            Utf8TomlReader lexer = Create("a = 1\nb = @\n");
            Drain(ref lexer);
        });

        // The invalid-number error is raised after the bare token "@" is consumed, at the terminating newline.
        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(6, ex.ColumnNumber);
        Assert.AreEqual(11, ex.Offset);
    }
}

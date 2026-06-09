// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Tests for the source-location diagnostics carried by <see cref="TomlFormatException" />, confirming that the parser
/// reports a stable, correct 1-based line and column (and 0-based offset) across LF and CRLF line endings, comment
/// lines, and multi-line strings.
/// </summary>
[TestClass]
public sealed class TomlReaderDiagnosticsTests
{
    /// <summary>
    /// Verifies that an error on the third LF-separated line reports line three.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidOnThirdLine_ForLf_ShouldReportLineThree()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\n\nkey = "));

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that line counting accounts for CRLF endings, reporting the second line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidOnSecondLine_ForCrlf_ShouldReportLineTwo()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\r\nbad ="));

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a comment line is counted, so an error on the following line reports line two.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidAfterComment_ShouldReportLineTwo()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("# comment\nbad ="));

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the interior newlines of a multi-line string are counted, so an error after it reports the correct
    /// line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidAfterMultilineString_ShouldReportCorrectLine()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = \"\"\"\nhello\n\"\"\"\nbad ="));

        Assert.AreEqual(4, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that an error at the very start of the source reports column one.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorAtLineStart_ShouldReportColumnOne()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("= 1"));

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(1, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that an error part-way along a line reports the 1-based column of the offending character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorMidLine_ShouldReportColumn()
    {
        // "a b": the key 'a' is read, then 'b' at column three is where an equals sign was expected.
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a b"));

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(3, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that the column resets to one at the start of a new LF-separated line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorOnSecondLine_ForLf_ShouldResetColumnToOne()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\n= 2"));

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(1, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a CRLF line ending does not inflate the next line's column — the carriage return is not counted as
    /// a column on the new line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorOnSecondLine_ForCrlf_ShouldReportColumnExcludingCarriageReturn()
    {
        // Line two is "ab c"; the 'c' at column four is where an equals sign was expected.
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("x = 1\r\nab c"));

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(4, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that the parse error carries the 0-based offset from the start of the source.
    /// </summary>
    [TestMethod]
    public void Parse_WhenError_ShouldReportOffset()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a b"));

        Assert.AreEqual(2, ex.Offset);
    }
}

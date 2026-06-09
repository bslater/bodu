// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Tests for the source-line diagnostics carried by <see cref="TomlFormatException" />, confirming that the parser
/// reports a stable, correct 1-based line number across LF and CRLF line endings, comment lines, and multi-line strings.
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
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\n\nkey = "));

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that line counting accounts for CRLF endings, reporting the second line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidOnSecondLine_ForCrlf_ShouldReportLineTwo()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\r\nbad ="));

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a comment line is counted, so an error on the following line reports line two.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidAfterComment_ShouldReportLineTwo()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("# comment\nbad ="));

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the interior newlines of a multi-line string are counted, so an error after it reports the correct
    /// line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidAfterMultilineString_ShouldReportCorrectLine()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = \"\"\"\nhello\n\"\"\"\nbad ="));

        Assert.AreEqual(4, ex.LineNumber);
    }
}

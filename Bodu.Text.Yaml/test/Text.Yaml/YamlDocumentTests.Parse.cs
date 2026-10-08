// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the position <see cref="YamlDocument.Parse(string)" /> reports when it rejects its input: the one-based
/// <see cref="YamlFormatException.LineNumber" /> and <see cref="YamlFormatException.ColumnNumber" /> and the
/// zero-based <see cref="YamlFormatException.Offset" /> point at the offending text, whichever line breaks precede it.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>
    /// Verifies that an error at the first character of the input, the reserved indicator <c>@</c>, is reported at line
    /// 1, column 1 and offset 0, so lines and columns count from one and offsets from zero.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorIsAtFirstCharacter_ShouldReportOneBasedLineAndColumn()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("@a");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(1, ex.ColumnNumber);
        Assert.AreEqual(0, ex.Offset);
    }

    /// <summary>
    /// Verifies that a malformed first line, one indented by a tab, is reported at line 1.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFirstLineIsMalformed_ShouldReportLineNumberOne()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("\tkey: value\n");
        });

        Assert.AreEqual(1, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that an alias with no name on the second line, <c>a:\n- b: *,</c>, is reported at line 2.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorIsOnSecondLine_ShouldReportLineTwo()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("a:\n- b: *,");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that an alias naming an undefined anchor, <c>a: *x</c>, is reported at the alias: line 1, column 4.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAliasNamesUndefinedAnchor_ShouldReportAliasPosition()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("a: *x");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(4, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a key a nested mapping repeats is reported on the line of the repeated key, line 4, rather than at
    /// the end of the mapping.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingKeyIsDuplicated_ShouldReportLineOfDuplicateKey()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("obj:\n  a: 1\n  b: 2\n  a: 3\n");
        });

        Assert.AreEqual(4, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a key a mapping repeats is reported at the start of the repeated key, line 3, column 1, rather
    /// than at its value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingKeyIsDuplicated_ShouldReportColumnOfDuplicateKey()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("a: 1\nb: 2\na: 3\n");
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(1, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a tab indenting the third line of a document whose lines end in CRLF is reported at line 3, each
    /// CRLF counting as one line break.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCrLfLineBreaksPrecedeError_ShouldReportErrorLine()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("a: 1\r\nb: 2\r\n\tc: 3\r\n");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that an unterminated flow sequence after a CRLF line break is reported at the same line and column,
    /// line 2, column 4, as after an LF line break.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCrLfLineBreaksPrecedeError_ShouldReportSameLineAsLf()
    {
        var crLf = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("foo\r\nbar: [");
        });

        var lf = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("foo\nbar: [");
        });

        Assert.AreEqual(2, crLf.LineNumber, "CRLF line number.");
        Assert.AreEqual(4, crLf.ColumnNumber, "CRLF column number.");
        Assert.AreEqual(2, lf.LineNumber, "LF line number.");
        Assert.AreEqual(4, lf.ColumnNumber, "LF column number.");
    }

    /// <summary>
    /// Verifies that the line of an error after two line breaks counts a CRLF pair as one break and a lone CR as one
    /// break, like an LF.
    /// </summary>
    /// <param name="yaml">The input: two line breaks, then an unterminated flow sequence.</param>
    /// <param name="expectedLine">The line the error is expected on.</param>
    [TestMethod]
    [DataRow("\r\n[", 2, DisplayName = "CRLF counts as one break")]
    [DataRow("\r\r[", 3, DisplayName = "CR CR counts as two breaks")]
    [DataRow("\n\n[", 3, DisplayName = "LF LF counts as two breaks")]
    public void Parse_WhenCrLfOrCrLineBreaksPrecedeError_ShouldCountEachBreakAsOneLine(string yaml, int expectedLine)
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });

        Assert.AreEqual(expectedLine, ex.LineNumber);
    }
}

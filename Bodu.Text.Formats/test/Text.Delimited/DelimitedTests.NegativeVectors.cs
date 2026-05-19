// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.NegativeVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> throws
    /// <see cref="DelimitedFormatException" /> when a quoted field is not closed before the end of the source.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedFieldIsUnterminated_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("name\n\"unclosed");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> throws
    /// <see cref="DelimitedFormatException" /> and reports <see cref="DelimitedFormatException.LineNumber" />
    /// as <c>1</c> when the unterminated quoted field is on the first line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnterminatedQuotedFieldOnFirstLine_ShouldReportLineNumberOne()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("\"unclosed");
        });

        Assert.AreEqual(1, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> reports the correct line number when the
    /// unterminated quoted field is not on the first line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnterminatedQuotedFieldOnThirdLine_ShouldReportLineNumberThree()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("a\n1\n\"unclosed");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the line number reported for an unterminated multiline quoted field is the line on which the
    /// opening quote appeared, not the line on which EOF was reached.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultilineQuotedFieldIsUnterminated_ShouldReportOpeningLineNumber()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("a\n\"line one\nline two");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

}

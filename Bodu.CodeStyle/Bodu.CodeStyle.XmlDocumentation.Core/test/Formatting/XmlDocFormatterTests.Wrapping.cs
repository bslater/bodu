// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Wrapping.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a short single-line <c>&lt;param&gt;</c> tag is kept on one line.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamIsShort_ShouldKeepSingleLine()
    {
        var input = "/// <param name=\"x\">The thing.</param>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that the formatter wraps prose text at the configured maximum line length, breaking at a word
    /// boundary.
    /// </summary>
    [TestMethod]
    public void Format_WhenLineExceedsMaxLength_ShouldWrapAtWordBoundary()
    {
        var longProse = string.Concat(System.Linq.Enumerable.Repeat("Lorem ipsum dolor sit amet, ", 6));
        var input =
            "/// <summary>\r\n" +
            "    /// " + longProse + "\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatOptions options = CreateOptions().WithMaxLineLength(80);
        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        foreach (var line in result.FormattedText.Split(["\r\n"], System.StringSplitOptions.None))
        {
            Assert.IsTrue(line.Length <= 80, $"Line '{line}' exceeds 80 characters.");
        }
    }

    /// <summary>
    /// Verifies that a trailing punctuation atom adjacent to an oversize inline atom (no whitespace between
    /// them) is kept on the same line as the atom, rather than being orphaned onto its own line by the wrapper.
    /// </summary>
    [TestMethod]
    public void Format_WhenTrailingPunctuationAdjacentToOversizeInlineAtom_ShouldStayOnSameLine()
    {
        var input =
            "/// <returns>\r\n" +
            "    /// The service collection passed to\r\n" +
            "    /// <see cref=\"ServiceCollectionExtensions.AddNotableDates(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)\" />.\r\n" +
            "    /// </returns>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed, "Already-canonical input should be reported as unchanged.");
        Assert.AreEqual(input, result.FormattedText);
        StringAssert.Contains(result.FormattedText, "string)\" />.");
        foreach (var line in result.FormattedText.Split(["\r\n"], System.StringSplitOptions.None))
        {
            Assert.AreNotEqual("    /// .", line, "Trailing '.' must not be orphaned onto its own line.");
        }
    }
}

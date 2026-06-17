// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.NormalizationOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that with <see cref="XmlDocFormatOptions.PreserveBlankLines" /> enabled a blank documentation
    /// line between two prose paragraphs inside <c>&lt;remarks&gt;</c> is retained verbatim.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveBlankLinesTrue_ShouldKeepBlankLineBetweenParagraphs()
    {
        XmlDocFormatOptions options = CreateOptions().WithPreserveBlankLines(true);

        var input =
            "/// <remarks>\r\n" +
            "    /// First paragraph.\r\n" +
            "    ///\r\n" +
            "    /// Second paragraph.\r\n" +
            "    /// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsFalse(result.Changed, "Blank lines should be preserved when PreserveBlankLines is enabled.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that with the default <see cref="XmlDocFormatOptions.PreserveBlankLines" /> (disabled) a blank
    /// documentation line is dropped and the surrounding prose is merged — the contrast case.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveBlankLinesFalse_ShouldDropBlankLineAndMergeProse()
    {
        var input =
            "/// <remarks>\r\n" +
            "    /// First paragraph.\r\n" +
            "    ///\r\n" +
            "    /// Second paragraph.\r\n" +
            "    /// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <remarks>\r\n" +
            "    /// First paragraph. Second paragraph.\r\n" +
            "    /// </remarks>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that with <see cref="XmlDocFormatOptions.CollapseProseWhitespace" /> disabled, multiple
    /// consecutive spaces between words on a line are preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Format_WhenCollapseProseWhitespaceFalse_ShouldPreserveMultipleSpaces()
    {
        XmlDocFormatOptions options = CreateOptions().WithCollapseProseWhitespace(false);

        var input =
            "/// <summary>\r\n" +
            "    /// Foo    bar.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsFalse(result.Changed, "Multiple spaces should be preserved when collapsing is disabled.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that with the default <see cref="XmlDocFormatOptions.CollapseProseWhitespace" /> (enabled),
    /// runs of consecutive spaces between words collapse to a single space — the contrast case.
    /// </summary>
    [TestMethod]
    public void Format_WhenCollapseProseWhitespaceTrue_ShouldCollapseMultipleSpaces()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// Foo    bar.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "    /// Foo bar.\r\n" +
            "    /// </summary>\r\n",
            result.FormattedText);
    }
}

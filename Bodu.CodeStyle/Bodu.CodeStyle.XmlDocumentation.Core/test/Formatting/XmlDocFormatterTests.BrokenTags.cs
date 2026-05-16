// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.BrokenTags.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that prose split across <c>///</c> lines inside a <c>&lt;summary&gt;</c> block is recombined
    /// into a single content line when it fits.
    /// </summary>
    [TestMethod]
    public void Format_WhenSummaryProseSplitAcrossLines_ShouldRejoinIntoSingleContentLine()
    {
        var input =
            "/// <summary>The thing.\r\n" +
            "    /// More text.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "    /// The thing. More text.\r\n" +
            "    /// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that a self-closing inline tag whose attributes are wrapped across <c>///</c> lines is
    /// rejoined into a single inline token on the same line as the surrounding prose.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineTagAttributesSplitAcrossLines_ShouldRejoinTag()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// See <see cref=\"Sample\"\r\n" +
            "    ///      langword=\"null\" /> for details.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "<see cref=\"Sample\" langword=\"null\" />");
    }

    /// <summary>
    /// Verifies that a paired inline tag (<c>&lt;c&gt;…&lt;/c&gt;</c>) whose body crosses a <c>///</c> boundary
    /// is collapsed back into a single inline token.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineCodeBodySplitAcrossLines_ShouldRejoinTag()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// Match <c>[A-Z]\r\n" +
            "    /// [0-9]+</c> for tokens.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        // Inside a paired inline tag, line breaks introduced by line-wrapping are stripped (not replaced with a
        // space) so the original content (e.g. a regex split for visual layout) is restored verbatim.
        StringAssert.Contains(result.FormattedText, "<c>[A-Z][0-9]+</c>");
    }

    /// <summary>
    /// Verifies that a block opening tag split mid-name across <c>///</c> lines is treated as malformed and
    /// left untouched.
    /// </summary>
    [TestMethod]
    public void Format_WhenBlockOpeningTagSplitMidName_ShouldLeaveUnchanged()
    {
        var input =
            "/// <sum\r\n" +
            "    /// mary>foo</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        // Mid-name split breaks the XML beyond repair; conservative behaviour leaves it alone.
        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a block closing tag split mid-name across <c>///</c> lines is treated as malformed and
    /// left untouched.
    /// </summary>
    [TestMethod]
    public void Format_WhenBlockClosingTagSplitMidName_ShouldLeaveUnchanged()
    {
        var input =
            "/// <summary>foo</summ\r\n" +
            "    /// ary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a <c>&lt;param&gt;</c> tag whose attribute name is split mid-token across <c>///</c>
    /// lines is permissively reassembled onto a single line, normalising the embedded newline to a space. The
    /// resulting attribute structure remains as-written (the formatter does not invent a corrected attribute
    /// name) but the output is line-friendly.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamAttributeNameSplitMidToken_ShouldRejoinTagOntoOneLine()
    {
        var input =
            "/// <param na\r\n" +
            "    /// me=\"x\">The thing.</param>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.IsFalse(result.FormattedText.Contains("\r\n    /// me="), "Tag should be rejoined onto a single line.");
        StringAssert.Contains(result.FormattedText, "The thing.");
        StringAssert.Contains(result.FormattedText, "</param>");
    }

    /// <summary>
    /// Verifies that a <c>&lt;param&gt;</c> tag whose <em>attribute value</em> is wrapped across <c>///</c>
    /// lines is rejoined and re-emitted on a single line.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamAttributeValueSplitAcrossLines_ShouldRejoinTag()
    {
        var input =
            "/// <param name=\"x\"\r\n" +
            "    ///        >The thing.</param>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "<param name=\"x\">");
        StringAssert.Contains(result.FormattedText, "The thing.");
        StringAssert.Contains(result.FormattedText, "</param>");
    }

    /// <summary>
    /// Verifies that <c>&lt;summary&gt;</c> + <c>&lt;remarks&gt;</c> with each opening tag at the end of one
    /// line and content on the next line is reformatted to canonical block layout.
    /// </summary>
    [TestMethod]
    public void Format_WhenOpeningTagAtEndOfLineWithContentOnNext_ShouldReformatToBlock()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// First line.\r\n" +
            "    /// Second line.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "    /// First line. Second line.\r\n" +
            "    /// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that a misformatted single-line <c>&lt;param&gt;</c> with extra spaces and broken indentation
    /// is normalised.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamHasExtraWhitespace_ShouldCollapseToCanonical()
    {
        var input = "/// <param   name=\"x\">  The   thing.   </param>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "The thing.");
        // Tag attributes are preserved with their internal whitespace; only prose whitespace is normalised.
        Assert.IsFalse(result.FormattedText.Contains("  The  "));
    }
}

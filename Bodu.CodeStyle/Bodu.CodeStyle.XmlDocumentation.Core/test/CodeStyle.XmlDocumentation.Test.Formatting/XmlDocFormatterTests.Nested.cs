// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Nested.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a <c>&lt;remarks&gt;</c> containing two <c>&lt;para&gt;</c> blocks emits each paragraph on
    /// its own pair of lines.
    /// </summary>
    [TestMethod]
    public void Format_WhenRemarksHasTwoParas_ShouldEmitEachOnItsOwnBlock()
    {
        var input =
            "/// <remarks><para>First.</para><para>Second.</para></remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "/// <remarks>");
        StringAssert.Contains(result.FormattedText, "/// <para>");
        StringAssert.Contains(result.FormattedText, "/// First.");
        StringAssert.Contains(result.FormattedText, "/// </para>");
        StringAssert.Contains(result.FormattedText, "/// Second.");
        StringAssert.Contains(result.FormattedText, "/// </remarks>");
    }

    /// <summary>
    /// Verifies that a <c>&lt;list&gt;</c> with <c>&lt;item&gt;</c> children preserves nested block structure.
    /// </summary>
    [TestMethod]
    public void Format_WhenListContainsItems_ShouldPreserveListStructure()
    {
        var input =
            "/// <remarks>\r\n" +
            "    /// <list type=\"bullet\">\r\n" +
            "    /// <item>\r\n" +
            "    /// <description>First.</description>\r\n" +
            "    /// </item>\r\n" +
            "    /// <item>\r\n" +
            "    /// <description>Second.</description>\r\n" +
            "    /// </item>\r\n" +
            "    /// </list>\r\n" +
            "    /// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "<list type=\"bullet\">");
        StringAssert.Contains(result.FormattedText, "<item>");
        StringAssert.Contains(result.FormattedText, "First.");
        StringAssert.Contains(result.FormattedText, "Second.");
    }

    /// <summary>
    /// Verifies that a table <c>&lt;list&gt;</c> collapses each <c>&lt;term&gt;</c> and
    /// <c>&lt;description&gt;</c> onto a single line while keeping <c>&lt;list&gt;</c>, <c>&lt;listheader&gt;</c>,
    /// and <c>&lt;item&gt;</c> as multiline block containers, producing a readable table.
    /// </summary>
    [TestMethod]
    public void Format_WhenTableListHasHeaderAndItems_ShouldEmitTermsAndDescriptionsOnSingleLines()
    {
        var input =
            "/// <list type=\"table\">\r\n" +
            "/// <listheader>\r\n" +
            "/// <term>\r\n" +
            "/// Combination\r\n" +
            "/// </term>\r\n" +
            "/// <description>\r\n" +
            "/// Yield\r\n" +
            "/// </description>\r\n" +
            "/// </listheader>\r\n" +
            "/// <item>\r\n" +
            "/// <term>\r\n" +
            "/// <see cref=\"SkipOnly\" />\r\n" +
            "/// </term>\r\n" +
            "/// <description>\r\n" +
            "/// No\r\n" +
            "/// </description>\r\n" +
            "/// </item>\r\n" +
            "/// </list>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.AreEqual(
            "/// <list type=\"table\">\r\n" +
            "/// <listheader>\r\n" +
            "/// <term>Combination</term>\r\n" +
            "/// <description>Yield</description>\r\n" +
            "/// </listheader>\r\n" +
            "/// <item>\r\n" +
            "/// <term><see cref=\"SkipOnly\" /></term>\r\n" +
            "/// <description>No</description>\r\n" +
            "/// </item>\r\n" +
            "/// </list>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that the readable single-line table layout is a fixed point: re-formatting it leaves it
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenTableListAlreadySingleLine_ShouldBeIdempotent()
    {
        var table =
            "/// <list type=\"table\">\r\n" +
            "/// <listheader>\r\n" +
            "/// <term>Combination</term>\r\n" +
            "/// <description>Yield</description>\r\n" +
            "/// </listheader>\r\n" +
            "/// <item>\r\n" +
            "/// <term><see cref=\"SkipOnly\" /></term>\r\n" +
            "/// <description>No</description>\r\n" +
            "/// </item>\r\n" +
            "/// </list>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(table, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.IsFalse(result.Changed, "The single-line table layout should be a fixed point.");
        Assert.AreEqual(table, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a <c>&lt;description&gt;</c> whose content overflows the line budget expands to the
    /// multiline block form (open and close tags on their own lines) so the content can wrap.
    /// </summary>
    [TestMethod]
    public void Format_WhenDescriptionExceedsWidth_ShouldExpandToMultilineBlock()
    {
        var longText = new string('x', 200);
        var input =
            "/// <list type=\"table\">\r\n" +
            "/// <item>\r\n" +
            "/// <description>" + longText + "</description>\r\n" +
            "/// </item>\r\n" +
            "/// </list>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        // The overflowing description must not stay on the single-line form; it expands so the open tag sits on
        // its own line ahead of the wrapped content.
        StringAssert.Contains(result.FormattedText, "/// <description>\r\n");
        StringAssert.Contains(result.FormattedText, "/// </description>\r\n");
    }

    /// <summary>
    /// Verifies that a force-multiline block containing only an inline atomic token still emits the atomic token
    /// on a content line between the open and close tags.
    /// </summary>
    [TestMethod]
    public void Format_WhenForceMultilineBlockContainsOnlyAtom_ShouldEmitAtomOnContentLine()
    {
        var input = "/// <summary><see cref=\"Sample\" /></summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "/// <summary>");
        StringAssert.Contains(result.FormattedText, "/// <see cref=\"Sample\" />");
        StringAssert.Contains(result.FormattedText, "/// </summary>");
    }

    /// <summary>
    /// Verifies that a force-multiline block with no content still emits the open and close tags on their own
    /// lines.
    /// </summary>
    [TestMethod]
    public void Format_WhenForceMultilineBlockIsEmpty_ShouldEmitOpenAndCloseTagsOnly()
    {
        var input = "/// <summary></summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "    /// </summary>\r\n",
            result.FormattedText);
    }
}

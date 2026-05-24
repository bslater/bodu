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

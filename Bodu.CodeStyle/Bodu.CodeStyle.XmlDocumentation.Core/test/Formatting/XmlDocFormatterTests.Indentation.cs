// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Indentation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a deeply-indented doc comment (eight-space base indent) is preserved when re-emitted.
    /// </summary>
    [TestMethod]
    public void Format_WhenBaseIndentIsEightSpaces_ShouldPreserveIt()
    {
        string input = "/// <summary>Foo.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: "        "), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "        /// Foo.");
    }

    /// <summary>
    /// Verifies that a doc comment with no base indent (top-level type) emits with no leading whitespace on the
    /// continuation lines.
    /// </summary>
    [TestMethod]
    public void Format_WhenBaseIndentIsEmpty_ShouldEmitNoLeadingWhitespace()
    {
        string input = "/// <summary>Foo.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "/// Foo.\r\n" +
            "/// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that a tab-indented doc comment's subsequent lines preserve the tab-based indent.
    /// </summary>
    [TestMethod]
    public void Format_WhenBaseIndentIsTab_ShouldPreserveTab()
    {
        string input =
            "/// <summary>\r\n" +
            "\t/// Foo.\r\n" +
            "\t/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: "\t"), CreateOptions());

        Assert.IsFalse(result.Changed);
    }
}

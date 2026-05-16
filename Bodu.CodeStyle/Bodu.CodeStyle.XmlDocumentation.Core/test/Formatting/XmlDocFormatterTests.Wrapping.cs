// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Wrapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
}

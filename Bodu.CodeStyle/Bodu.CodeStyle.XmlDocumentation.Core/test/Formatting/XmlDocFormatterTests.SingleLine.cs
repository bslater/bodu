// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.SingleLine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that <c>&lt;typeparam&gt;</c> stays on a single line when its content is short.
    /// </summary>
    [TestMethod]
    public void Format_WhenTypeParamIsShort_ShouldKeepSingleLine()
    {
        string input = "/// <typeparam name=\"T\">The element type.</typeparam>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
    }

    /// <summary>
    /// Verifies that <c>&lt;returns&gt;</c> stays on a single line when its content is short.
    /// </summary>
    [TestMethod]
    public void Format_WhenReturnsIsShort_ShouldKeepSingleLine()
    {
        string input = "/// <returns>The computed result.</returns>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
    }

    /// <summary>
    /// Verifies that <c>&lt;exception&gt;</c> stays on a single line when its content is short.
    /// </summary>
    [TestMethod]
    public void Format_WhenExceptionIsShort_ShouldKeepSingleLine()
    {
        string input = "/// <exception cref=\"System.ArgumentNullException\">When the input is null.</exception>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
    }

    /// <summary>
    /// Verifies that a long <c>&lt;param&gt;</c> tag is expanded onto open/content/close lines.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamIsLong_ShouldExpandToMultipleLines()
    {
        string longText = string.Concat(System.Linq.Enumerable.Repeat("very long parameter description ", 8));
        string input = "/// <param name=\"x\">" + longText + "</param>\r\n";

        XmlDocFormatOptions options = CreateOptions().WithMaxLineLength(60);
        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "/// <param name=\"x\">");
        StringAssert.Contains(result.FormattedText, "/// </param>");
    }

    /// <summary>
    /// Verifies that a short single-line <c>&lt;value&gt;</c> tag is kept on one line.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueIsShort_ShouldKeepSingleLine()
    {
        string input = "/// <value>The current state.</value>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
    }
}

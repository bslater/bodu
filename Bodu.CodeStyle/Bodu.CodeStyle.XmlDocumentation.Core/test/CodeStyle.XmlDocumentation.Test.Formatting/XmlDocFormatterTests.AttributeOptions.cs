// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.AttributeOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that with <see cref="XmlDocFormatOptions.PreserveXmlTagAttributes" /> enabled a tag authored
    /// across multiple lines is preserved verbatim — its line breaks and alignment whitespace are kept, with
    /// surrounding prose still flowing into the tag's first line and continuing from its last line.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveXmlTagAttributesTrue_ShouldPreserveMultiLineTagLayout()
    {
        XmlDocFormatOptions options = CreateOptions().WithPreserveXmlTagAttributes(true);

        var input =
            "/// <summary>\r\n" +
            "/// See <see cref=\"Sample\"\r\n" +
            "///      langword=\"null\" /> for details.\r\n" +
            "/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), options);

        Assert.IsFalse(result.Changed, "A verbatim-preserved multi-line tag should be left unchanged.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that with the default <see cref="XmlDocFormatOptions.PreserveXmlTagAttributes" /> (disabled) a
    /// multi-line tag is reflowed onto a single line — the contrast case.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveXmlTagAttributesFalse_ShouldReflowMultiLineTagToSingleLine()
    {
        var input =
            "/// <summary>\r\n" +
            "/// See <see cref=\"Sample\"\r\n" +
            "///      langword=\"null\" /> for details.\r\n" +
            "/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.AreEqual(
            "/// <summary>\r\n" +
            "/// See <see cref=\"Sample\" langword=\"null\" /> for details.\r\n" +
            "/// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that with the default <see cref="XmlDocFormatOptions.PreserveCrefText" /> (enabled) the
    /// whitespace inside an attribute value (such as a <c>cref</c>) is preserved when the tag is reflowed.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveCrefTextTrue_ShouldKeepWhitespaceInsideCref()
    {
        var input =
            "/// <summary>\r\n" +
            "/// Text <see cref=\"M(int,   string)\"\r\n" +
            "/// /> end.\r\n" +
            "/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.AreEqual(
            "/// <summary>\r\n" +
            "/// Text <see cref=\"M(int,   string)\" /> end.\r\n" +
            "/// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that with <see cref="XmlDocFormatOptions.PreserveCrefText" /> disabled the whitespace inside an
    /// attribute value collapses to a single space when the tag is reflowed — the contrast case.
    /// </summary>
    [TestMethod]
    public void Format_WhenPreserveCrefTextFalse_ShouldCollapseWhitespaceInsideCref()
    {
        XmlDocFormatOptions options = CreateOptions().WithPreserveCrefText(false);

        var input =
            "/// <summary>\r\n" +
            "/// Text <see cref=\"M(int,   string)\"\r\n" +
            "/// /> end.\r\n" +
            "/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), options);

        Assert.AreEqual(
            "/// <summary>\r\n" +
            "/// Text <see cref=\"M(int, string)\" /> end.\r\n" +
            "/// </summary>\r\n",
            result.FormattedText);
    }
}

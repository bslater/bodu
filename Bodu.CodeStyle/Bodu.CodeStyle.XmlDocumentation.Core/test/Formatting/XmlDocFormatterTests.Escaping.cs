// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Escaping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that XML entity references inside prose are preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Format_WhenProseContainsEntityReferences_ShouldPreserveThem()
    {
        string input =
            "/// <summary>\r\n" +
            "    /// Use &amp; for ampersand and &lt;tag&gt; for raw markup.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "&amp;");
        StringAssert.Contains(result.FormattedText, "&lt;tag&gt;");
    }

    /// <summary>
    /// Verifies that CDATA sections are preserved verbatim and that the markup inside the section is not parsed
    /// as XML tags.
    /// </summary>
    [TestMethod]
    public void Format_WhenCDataSectionPresent_ShouldPreserveVerbatim()
    {
        string input =
            "/// <example>\r\n" +
            "    /// <![CDATA[var x = new Foo<int>();]]>\r\n" +
            "    /// </example>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "<![CDATA[var x = new Foo<int>();]]>");
    }

    /// <summary>
    /// Verifies that XML comments inside a documentation comment are preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Format_WhenXmlCommentPresent_ShouldPreserveVerbatim()
    {
        string input =
            "/// <summary>\r\n" +
            "    /// Foo. <!-- internal note --> Bar.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "<!-- internal note -->");
    }

    /// <summary>
    /// Verifies that attribute values inside cref strings are preserved verbatim including dotted and generic
    /// type names.
    /// </summary>
    [TestMethod]
    public void Format_WhenCrefContainsGenericTypeName_ShouldPreserveCrefText()
    {
        string input =
            "/// <summary>\r\n" +
            "    /// See <see cref=\"System.Collections.Generic.IEnumerable{T}\" />.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "cref=\"System.Collections.Generic.IEnumerable{T}\"");
    }

    /// <summary>
    /// Verifies that single-quoted attribute values inside a self-closing inline tag are preserved.
    /// </summary>
    [TestMethod]
    public void Format_WhenSelfClosingTagUsesSingleQuotes_ShouldPreserveQuotes()
    {
        string input = "/// <summary>See <see cref='Sample' />.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "cref='Sample'");
    }
}

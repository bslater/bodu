// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.BrokenTagIdempotency.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that once a broken inline tag has been rejoined by a first formatting pass, a second pass leaves
    /// it unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenRejoinedInlineTag_ShouldBeIdempotentOnSecondPass()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// See <see cref=\"X\"\r\n" +
            "    ///      langword=\"null\" />.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.AreEqual(first.FormattedText, second.FormattedText);
        Assert.IsFalse(second.Changed);
    }

    /// <summary>
    /// Verifies that once a broken-prose summary has been rejoined by a first formatting pass, a second pass
    /// leaves it unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenRejoinedBrokenProse_ShouldBeIdempotentOnSecondPass()
    {
        var input =
            "/// <summary>The thing.\r\n" +
            "    /// More text.</summary>\r\n";

        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.AreEqual(first.FormattedText, second.FormattedText);
        Assert.IsFalse(second.Changed);
    }

    /// <summary>
    /// Verifies that a malformed-name doc comment (left unchanged on the first pass) is also left unchanged on
    /// the second pass — the formatter never claims it has changed an input it cannot parse.
    /// </summary>
    [TestMethod]
    public void Format_WhenMalformedTagNameSplit_ShouldStayUnchangedAcrossPasses()
    {
        var input =
            "/// <sum\r\n" +
            "    /// mary>foo</summary>\r\n";

        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.IsFalse(first.Changed);
        Assert.IsFalse(second.Changed);
        Assert.AreEqual(input, first.FormattedText);
        Assert.AreEqual(input, second.FormattedText);
    }

    /// <summary>
    /// Verifies that a long inline cref reference whose attribute value has been wrapped is rejoined and then
    /// stable across repeated passes.
    /// </summary>
    [TestMethod]
    public void Format_WhenLongCrefWrapped_ShouldRejoinAndBeIdempotent()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// See <see\r\n" +
            "    ///     cref=\"System.Collections.Generic.IEnumerable{T}\" />.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.IsTrue(first.Changed);
        Assert.IsFalse(second.Changed);
        Assert.AreEqual(first.FormattedText, second.FormattedText);
        StringAssert.Contains(first.FormattedText, "<see cref=\"System.Collections.Generic.IEnumerable{T}\" />");
    }
}

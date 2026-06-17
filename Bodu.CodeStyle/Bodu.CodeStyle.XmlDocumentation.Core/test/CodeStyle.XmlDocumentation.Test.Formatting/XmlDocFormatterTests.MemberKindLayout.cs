// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.MemberKindLayout.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    private static XmlDocFormatContext CreateFieldContext() =>
        new XmlDocFormatContext(string.Empty, "\r\n", XmlDocMemberKindHint.Field);

    /// <summary>
    /// Verifies that a short single-line <c>&lt;summary&gt;</c> on a field is left on a single line rather than
    /// expanded to the multiline block form used for other members.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldSummaryIsShortSingleLine_ShouldRemainSingleLine()
    {
        var input = "/// <summary>Holds the running total.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateFieldContext(), CreateOptions());

        Assert.IsFalse(result.Changed, "A short field summary should stay on a single line.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a multi-line <c>&lt;summary&gt;</c> on a field is collapsed back onto a single line when it
    /// fits the width budget.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldSummaryIsMultiLineButShort_ShouldCollapseToSingleLine()
    {
        var input =
            "/// <summary>\r\n" +
            "/// Holds the running total.\r\n" +
            "/// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateFieldContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual("/// <summary>Holds the running total.</summary>\r\n", result.FormattedText);
    }

    /// <summary>
    /// Verifies that a field <c>&lt;summary&gt;</c> whose content exceeds the width budget still expands to the
    /// multiline block form.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldSummaryExceedsWidth_ShouldExpandToMultiline()
    {
        XmlDocFormatOptions options = CreateOptions().WithMaxLineLength(40);

        var input = "/// <summary>Holds the running total accumulated across the whole batch.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateFieldContext(), options);

        Assert.IsTrue(result.Changed);
        StringAssert.StartsWith(result.FormattedText, "/// <summary>\r\n");
        StringAssert.Contains(result.FormattedText, "/// </summary>\r\n");
    }

    /// <summary>
    /// Verifies that a short single-line <c>&lt;summary&gt;</c> on a non-field member (here a method) still
    /// expands to the multiline block form — the field rule does not affect other member kinds.
    /// </summary>
    [TestMethod]
    public void Format_WhenMethodSummaryIsShortSingleLine_ShouldExpandToMultiline()
    {
        var input = "/// <summary>Holds the running total.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(
            input,
            new XmlDocFormatContext(string.Empty, "\r\n", XmlDocMemberKindHint.Method),
            CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "/// Holds the running total.\r\n" +
            "/// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that with <see cref="XmlDocFormatOptions.KeepFieldSummaryOnSingleLine" /> enabled a field
    /// <c>&lt;summary&gt;</c> whose content exceeds the width budget is kept on a single line rather than expanded
    /// to the multiline block form — the contrast case to
    /// <see cref="Format_WhenFieldSummaryExceedsWidth_ShouldExpandToMultiline" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldSummaryExceedsWidthAndKeepSingleLineEnabled_ShouldNotWrap()
    {
        XmlDocFormatOptions options = CreateOptions().WithMaxLineLength(40).WithKeepFieldSummaryOnSingleLine(true);

        var input = "/// <summary>Holds the running total accumulated across the whole batch.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateFieldContext(), options);

        Assert.IsFalse(result.Changed, "An overflowing field summary should be kept on its single line.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocFormatOptions.KeepFieldSummaryOnSingleLine" /> only affects fields: a
    /// method <c>&lt;summary&gt;</c> that exceeds the width budget still expands to the multiline block form even
    /// when the option is enabled.
    /// </summary>
    [TestMethod]
    public void Format_WhenMethodSummaryExceedsWidthAndKeepSingleLineEnabled_ShouldStillExpand()
    {
        XmlDocFormatOptions options = CreateOptions().WithMaxLineLength(40).WithKeepFieldSummaryOnSingleLine(true);

        var input = "/// <summary>Holds the running total accumulated across the whole batch.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(
            input,
            new XmlDocFormatContext(string.Empty, "\r\n", XmlDocMemberKindHint.Method),
            options);

        Assert.IsTrue(result.Changed);
        StringAssert.StartsWith(result.FormattedText, "/// <summary>\r\n");
    }
}

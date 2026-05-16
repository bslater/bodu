// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Idempotency.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a single-line summary, once formatted, is stable under repeated formatting.
    /// </summary>
    [TestMethod]
    public void Format_WhenSingleLineSummaryRunRepeatedly_ShouldBeIdempotent()
    {
        AssertIdempotent("/// <summary>Foo bar.</summary>\r\n");
    }

    /// <summary>
    /// Verifies that an already-canonical multi-line summary is stable under repeated formatting.
    /// </summary>
    [TestMethod]
    public void Format_WhenCanonicalSummaryRunRepeatedly_ShouldBeIdempotent()
    {
        AssertIdempotent(
            "/// <summary>\r\n" +
            "    /// Foo bar.\r\n" +
            "    /// </summary>\r\n");
    }

    /// <summary>
    /// Verifies that nested <c>&lt;remarks&gt;</c> with <c>&lt;para&gt;</c> blocks survive repeated formatting.
    /// </summary>
    [TestMethod]
    public void Format_WhenRemarksWithParaRunRepeatedly_ShouldBeIdempotent()
    {
        AssertIdempotent(
            "/// <remarks>\r\n" +
            "    /// <para>\r\n" +
            "    /// First paragraph.\r\n" +
            "    /// </para>\r\n" +
            "    /// <para>\r\n" +
            "    /// Second paragraph.\r\n" +
            "    /// </para>\r\n" +
            "    /// </remarks>\r\n");
    }

    /// <summary>
    /// Verifies that single-line <c>&lt;param&gt;</c> tags are stable under repeated formatting.
    /// </summary>
    [TestMethod]
    public void Format_WhenParamSingleLineRunRepeatedly_ShouldBeIdempotent()
    {
        AssertIdempotent("/// <param name=\"x\">The thing.</param>\r\n");
    }

    /// <summary>
    /// Verifies that an inline <c>&lt;see /&gt;</c> token is stable under repeated formatting.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineSeeRunRepeatedly_ShouldBeIdempotent()
    {
        AssertIdempotent(
            "/// <summary>\r\n" +
            "    /// See <see cref=\"Sample\" /> for details.\r\n" +
            "    /// </summary>\r\n");
    }

    /// <summary>
    /// Verifies that misformatted input becomes idempotent on the second pass after formatting once.
    /// </summary>
    [TestMethod]
    public void Format_WhenMisformattedThenFormattedRunRepeatedly_ShouldBeIdempotent()
    {
        var input = "/// <summary>Foo.</summary>\r\n";
        XmlDocFormatResult first = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());
        XmlDocFormatResult second = CreateFormatter().FormatTrivia(first.FormattedText, CreateContext(), CreateOptions());
        XmlDocFormatResult third = CreateFormatter().FormatTrivia(second.FormattedText, CreateContext(), CreateOptions());

        Assert.AreEqual(first.FormattedText, second.FormattedText);
        Assert.AreEqual(second.FormattedText, third.FormattedText);
        Assert.IsFalse(second.Changed);
        Assert.IsFalse(third.Changed);
    }

    private static void AssertIdempotent(string input)
    {
        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.AreEqual(first.FormattedText, second.FormattedText, "Second pass output differs from first pass.");
        Assert.IsFalse(second.Changed, "Already-formatted text should report Changed=false.");
    }
}

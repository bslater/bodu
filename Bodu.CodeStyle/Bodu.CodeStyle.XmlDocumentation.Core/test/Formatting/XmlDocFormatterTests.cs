// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public partial class XmlDocFormatterTests
{
    private static XmlDocFormatter CreateFormatter() => new XmlDocFormatter();

    private static XmlDocFormatOptions CreateOptions() => XmlDocFormatPolicyDefaults.CreateBoduDefaults();

    private static XmlDocFormatContext CreateContext(string baseIndent = "    ", string lineEnding = "\r\n") =>
        new XmlDocFormatContext(baseIndent, lineEnding, XmlDocMemberKindHint.Method);

    /// <summary>
    /// Verifies that running the formatter on already-canonical content returns the input unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenAlreadyFormatted_ShouldReturnUnchanged()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed, "Already-canonical input should be reported as unchanged.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a single-line summary is expanded into the canonical three-line block form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Format_WhenSummaryIsSingleLine_ShouldExpandToMultiline()
    {
        var input = "/// <summary>Foo bar.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <summary>\r\n" +
            "    /// Foo bar.\r\n" +
            "    /// </summary>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that running the formatter twice produces byte-identical output, satisfying the idempotency contract.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Format_WhenRunTwice_ShouldBeIdempotent()
    {
        var input = "/// <summary>Lorem ipsum dolor.</summary>\r\n";

        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatOptions options = CreateOptions();
        XmlDocFormatContext context = CreateContext();

        XmlDocFormatResult first = formatter.FormatTrivia(input, context, options);
        XmlDocFormatResult second = formatter.FormatTrivia(first.FormattedText, context, options);

        Assert.AreEqual(first.FormattedText, second.FormattedText);
        Assert.IsFalse(second.Changed);
    }

    /// <summary>
    /// Verifies that malformed XML doc trivia is left untouched and reported as unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenMalformedXmlDoc_ShouldLeaveUnchangedAndReportNoChange()
    {
        var input = "/// <summary>Foo bar.\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
        Assert.AreEqual(0, result.Changes.Length);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Drives <see cref="RuleParseKat{TDocument}" /> and <see cref="InvalidRuleParseKat" /> against the
/// public <see cref="NotableDateRuleParser" />. Each row's <c>Name</c> is surfaced in failure
/// diagnostics so a regression points at the specific scenario. Bespoke parser coverage (every XML
/// construct, every adjustment shape, override bodies, optional month-day handling) lives in the
/// existing <c>NotableDateRuleParserTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class NotableDateRuleParserKatTests
{
    private const string s_namespaceDeclaration = " xmlns=\"urn:bodu:globalization:calendar\"";

    private static IReadOnlyList<RuleParseKat<int>> ValidKats { get; } =
    [
        new(
            Name: "empty document yields zero local rules",
            Input: $"<NotableDates{s_namespaceDeclaration} />",
            Format: "xml",
            Expected: 0),

        new(
            Name: "single fixed-date rule with category",
            Input:
                $"<NotableDates{s_namespaceDeclaration}>" +
                "  <NotableDate name=\"New Year\">" +
                "    <Rule name=\"NewYearRule\" category=\"Holiday\">" +
                "      <Fixed month=\"1\" day=\"1\" />" +
                "    </Rule>" +
                "  </NotableDate>" +
                "</NotableDates>",
            Format: "xml",
            Expected: 1),

        new(
            Name: "two fixed-date rules",
            Input:
                $"<NotableDates{s_namespaceDeclaration}>" +
                "  <NotableDate name=\"New Year\"><Rule name=\"NewYearRule\" category=\"Holiday\"><Fixed month=\"1\" day=\"1\" /></Rule></NotableDate>" +
                "  <NotableDate name=\"Christmas\"><Rule name=\"ChristmasRule\" category=\"Holiday\"><Fixed month=\"12\" day=\"25\" /></Rule></NotableDate>" +
                "</NotableDates>",
            Format: "xml",
            Expected: 2),
    ];

    private static IReadOnlyList<InvalidRuleParseKat> InvalidKats { get; } =
    [
        new(
            Name: "empty payload",
            Input: string.Empty,
            Format: "xml",
            ExceptionType: typeof(ArgumentNullException)),

        new(
            Name: "whitespace payload",
            Input: "   ",
            Format: "xml",
            ExceptionType: typeof(ArgumentNullException)),
    ];

    /// <summary>
    /// Verifies that every <see cref="RuleParseKat{TDocument}" /> row parses to a document whose
    /// <see cref="ParsedNotableDateDocument.LocalRules" /> count matches the expected value. Each row
    /// asserts one observable outcome on the document the parser hands back.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenKatIsValid_ShouldYieldExpectedLocalRuleCount()
    {
        foreach (RuleParseKat<int> kat in ValidKats)
        {
            ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(kat.Input);

            Assert.AreEqual(
                kat.Expected,
                document.LocalRules.Length,
                $"Rule-parse KAT '{kat.Name}': local rule count mismatch.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="InvalidRuleParseKat" /> row causes
    /// <see cref="NotableDateRuleParser.ParseDocument(string)" /> to throw the documented exception
    /// type.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenKatIsInvalid_ShouldThrowExpectedException()
    {
        foreach (InvalidRuleParseKat kat in InvalidKats)
        {
            Exception? captured = null;
            try
            {
                _ = NotableDateRuleParser.ParseDocument(kat.Input);
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            Assert.IsNotNull(captured, $"Invalid rule-parse KAT '{kat.Name}': parser must throw.");
            Assert.AreEqual(
                kat.ExceptionType,
                captured.GetType(),
                $"Invalid rule-parse KAT '{kat.Name}': wrong exception type.");
        }
    }
}

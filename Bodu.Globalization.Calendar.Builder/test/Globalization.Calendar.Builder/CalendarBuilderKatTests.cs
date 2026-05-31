// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarBuilderKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Drives <see cref="CalendarBuilderOutputKat" /> and <see cref="CalendarBuilderInvalidKat" /> against
/// the fluent <see cref="NotableDateDocumentBuilder" /> surface. The plan calls these "source generator"
/// KATs but the Bodu calendar builder is a fluent builder — not a Roslyn source generator — so each
/// positive row treats <c>SourceInput</c> as a builder-produced XML payload that must parse back to
/// the expected rule count, and each negative row treats <c>SourceInput</c> as a malformed XML payload
/// the parser must reject. Bespoke fluent-builder coverage (clone semantics, JSON serialisation,
/// adjustment-rule composition) remains in <c>NotableDateDocumentBuilderTests.*</c>.
/// </summary>
[TestClass]
public sealed class CalendarBuilderKatTests
{
    private static readonly string s_singleStaticRule = NotableDateDocumentBuilder.Create()
        .AddDate("New Year", b => b.AddRule(r => r.Fixed(1, 1)))
        .ToXDocument()
        .ToString();

    private static readonly string s_twoStaticRules = NotableDateDocumentBuilder.Create()
        .AddDate("New Year", b => b.AddRule(r => r.Fixed(1, 1)))
        .AddDate("Christmas", b => b.AddRule(r => r.Fixed(12, 25)))
        .ToXDocument()
        .ToString();

    private static IReadOnlyList<CalendarBuilderOutputKat> OutputKats { get; } =
    [
        new(
            Name: "single static rule",
            SourceInput: s_singleStaticRule,
            ExpectedResourceName: "notable-dates",
            ExpectedRuleCount: 1),

        new(
            Name: "two static rules",
            SourceInput: s_twoStaticRules,
            ExpectedResourceName: "notable-dates",
            ExpectedRuleCount: 2),
    ];

    private static IReadOnlyList<CalendarBuilderInvalidKat> InvalidKats { get; } =
    [
        new(
            Name: "empty XML payload",
            SourceInput: "",
            ExpectedDiagnosticId: "BC001",
            MessageContains: null),

        new(
            Name: "non-XML text payload",
            SourceInput: "not valid xml",
            ExpectedDiagnosticId: "BC001",
            MessageContains: null),
    ];

    /// <summary>
    /// Verifies that each <see cref="CalendarBuilderOutputKat" /> row, when parsed back through the
    /// public <see cref="NotableDateRuleParser" />, produces the expected rule count. Documents that
    /// fluent-builder output round-trips through the parser without loss.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenSourceInputIsKnownBuilderOutput_ShouldYieldExpectedRuleCount()
    {
        foreach (CalendarBuilderOutputKat kat in OutputKats)
        {
            List<NotableDateRule> rules = NotableDateRuleParser.ParseXml(kat.SourceInput);

            Assert.AreEqual(
                kat.ExpectedRuleCount,
                rules.Count,
                $"Builder output KAT '{kat.Name}': parsed rule count mismatch.");
        }
    }

    /// <summary>
    /// Verifies that each <see cref="CalendarBuilderInvalidKat" /> row causes the parser to throw —
    /// either with an empty XML payload (which the parser rejects with <see cref="ArgumentException" />)
    /// or with malformed XML (rejected as <see cref="System.Xml.XmlException" /> / <see cref="ArgumentException" />).
    /// Documents that malformed builder output is rejected by the validation layer.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenSourceInputIsMalformed_ShouldThrow()
    {
        foreach (CalendarBuilderInvalidKat kat in InvalidKats)
        {
            bool threw = false;
            try
            {
                _ = NotableDateRuleParser.ParseXml(kat.SourceInput);
            }
            catch
            {
                threw = true;
            }

            Assert.IsTrue(threw, $"Invalid builder KAT '{kat.Name}': parser must throw.");
        }
    }
}

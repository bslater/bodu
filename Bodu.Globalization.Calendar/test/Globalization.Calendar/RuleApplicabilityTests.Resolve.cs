// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class RuleApplicabilityTests
{
    /// <summary>
    /// Verifies that a bounded <c>fromYear</c>/<c>toYear</c> rule is emitted only within its window when resolved through
    /// the service.
    /// </summary>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <param name="expected">Whether the windowed concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow(2019, false)]
    [DataRow(2020, true)]
    [DataRow(2025, true)]
    [DataRow(2030, true)]
    [DataRow(2031, false)]
    public void Resolve_WhenWindowedRule_EmitsOnlyWithinWindow(int year, bool expected)
    {
        Assert.AreEqual(expected, Emits("windowed", "XX", year));
    }

    /// <summary>
    /// Verifies that an <c>OnlyYear</c> rule is emitted exactly for the listed years and suppressed otherwise when
    /// resolved through the service.
    /// </summary>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <param name="expected">Whether the only-years concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow(2024, true)]   // listed
    [DataRow(2025, false)]  // unlisted
    [DataRow(2026, true)]   // listed
    public void Resolve_WhenOnlyYearsRule_EmitsAccordingToSet(int year, bool expected)
    {
        Assert.AreEqual(expected, Emits("only-years", "XX", year));
    }

    /// <summary>
    /// Verifies that an <c>ExceptYear</c> rule is suppressed for the listed year and emitted otherwise when resolved
    /// through the service.
    /// </summary>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <param name="expected">Whether the except-2025 concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow(2024, true)]   // not excepted
    [DataRow(2025, false)]  // excepted
    public void Resolve_WhenExceptYearRule_EmitsAccordingToSet(int year, bool expected)
    {
        Assert.AreEqual(expected, Emits("except-2025", "XX", year));
    }

    /// <summary>
    /// Verifies that a windowed rule with an <c>ExceptYear</c> is suppressed for the excepted year and for years outside
    /// the window, and emitted otherwise, when resolved through the service.
    /// </summary>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <param name="expected">Whether the windowed-except concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow(2024, true)]   // inside window, not excepted
    [DataRow(2025, false)]  // excepted year
    [DataRow(2031, false)]  // outside window
    public void Resolve_WhenWindowedExceptRule_EmitsAccordingToSet(int year, bool expected)
    {
        Assert.AreEqual(expected, Emits("windowed-except", "XX", year));
    }

    /// <summary>
    /// Verifies that a national rule is emitted for the national request and its subdivisions but not for a foreign
    /// territory, when resolved through the service.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="expected">Whether the national concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow("AU", true)]
    [DataRow("AU-NSW", true)]
    [DataRow("US", false)]
    public void Resolve_WhenNationalRule_EmitsForNationAndSubdivisions(string territory, bool expected)
    {
        Assert.AreEqual(expected, Emits("national-au", territory, 2026));
    }

    /// <summary>
    /// Verifies that a subnational-only rule is emitted for its subdivision but not for the national request, when
    /// resolved through the service.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="expected">Whether the subnational-nsw concept is expected to be emitted.</param>
    [TestMethod]
    [DataRow("AU-NSW", true)]  // the scoped subdivision
    [DataRow("AU", false)]     // the national request
    public void Resolve_WhenSubnationalRule_EmitsOnlyForSubdivision(string territory, bool expected)
    {
        Assert.AreEqual(expected, Emits("subnational-nsw", territory, 2026));
    }

    /// <summary>
    /// Verifies that when a concept carries both a national and a narrower subdivision rule, the scoped subdivision emits
    /// the narrower rule (it shadows the national one) while any other subdivision falls back to the national rule.
    /// </summary>
    /// <param name="territory">The requested subdivision code.</param>
    /// <param name="expectedRuleId">The rule id expected to win for the request.</param>
    [TestMethod]
    [DataRow("AU-NSW", "nsw")]       // scoped subdivision shadows the national rule
    [DataRow("AU-VIC", "national")]  // other subdivision falls back to the national rule
    public void Resolve_WhenTerritoryShadowing_NarrowerRuleShadowsNationalRule(string territory, string expectedRuleId)
    {
        DateRange range = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        NotableDate match = CreateService().Resolve(range, territory).Single(r => r.NotableDateId == "shadowed");

        Assert.AreEqual(expectedRuleId, match.RuleId);
    }
}

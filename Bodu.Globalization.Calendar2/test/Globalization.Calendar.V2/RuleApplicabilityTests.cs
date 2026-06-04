// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the <see cref="RuleApplicability" /> truth tables: the year window (<c>fromYear</c>/<c>toYear</c>), the
/// explicit inclusion (<c>OnlyYear</c>) and exclusion (<c>ExceptYear</c>) sets, and the territory matching and
/// specificity rules, both directly and end-to-end through a filtered service resolve.
/// </summary>
/// <remarks>
/// <para>
/// The v1 applicability test exercised <c>FirstYear</c>/<c>LastYear</c> together with an <c>OccurrenceYears</c> modulo
/// cadence; v2 has no cadence and replaces it with explicit <see cref="RuleApplicability.OnlyYears" /> and
/// <see cref="RuleApplicability.ExceptYears" /> sets, so the cadence rows are not ported and the inclusion/exclusion
/// rows are added in their place.
/// </para>
/// </remarks>
[TestClass]
public sealed class RuleApplicabilityTests
{
    /// <summary>
    /// Builds a <see cref="RuleApplicability" /> with the supplied year bounds and inclusion/exclusion sets for a global
    /// (territory-unscoped) rule.
    /// </summary>
    /// <param name="fromYear">The lower year bound, or <see langword="null" /> for none.</param>
    /// <param name="toYear">The upper year bound, or <see langword="null" /> for none.</param>
    /// <param name="onlyYears">The explicit inclusion years.</param>
    /// <param name="exceptYears">The explicit exclusion years.</param>
    /// <returns>The constructed applicability.</returns>
    private static RuleApplicability Years(int? fromYear, int? toYear, int[]? onlyYears = null, int[]? exceptYears = null) =>
        new(CalendarSystem.Gregorian, fromYear, toYear, [], onlyYears ?? [], exceptYears ?? []);

    /// <summary>
    /// Builds a <see cref="RuleApplicability" /> scoped to the supplied territories with no year restriction.
    /// </summary>
    /// <param name="territories">The territory codes the rule applies to.</param>
    /// <returns>The constructed applicability.</returns>
    private static RuleApplicability Territories(params string[] territories) =>
        new(CalendarSystem.Gregorian, null, null, territories, [], []);

    // -----------------------------------------------------------------------------------------------------------
    // AppliesTo — year window truth table
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.AppliesTo" /> enforces the inclusive <c>fromYear</c>/<c>toYear</c>
    /// window, returning the expected result for each year relative to the bounds.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="fromYear">The lower bound, or <c>-1</c> for none.</param>
    /// <param name="toYear">The upper bound, or <c>-1</c> for none.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2025, -1, -1, true)]    // unbounded
    [DataRow(2025, 2020, 2030, true)] // interior
    [DataRow(2020, 2020, 2030, true)] // lower boundary inclusive
    [DataRow(2030, 2020, 2030, true)] // upper boundary inclusive
    [DataRow(2019, 2020, 2030, false)] // below fromYear
    [DataRow(2031, 2020, 2030, false)] // above toYear
    [DataRow(2025, 2025, -1, true)]   // lower bound only, on boundary
    [DataRow(2024, 2025, -1, false)]  // lower bound only, below
    [DataRow(2025, -1, 2025, true)]   // upper bound only, on boundary
    [DataRow(2026, -1, 2025, false)]  // upper bound only, above
    public void AppliesTo_WhenYearWindow_ShouldMatchExpected(int year, int fromYear, int toYear, bool expected)
    {
        RuleApplicability applicability = Years(
            fromYear < 0 ? null : fromYear,
            toYear < 0 ? null : toYear);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    // -----------------------------------------------------------------------------------------------------------
    // AppliesTo — OnlyYears / ExceptYears truth table
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.OnlyYears" /> restricts applicability to the listed years and excludes
    /// every other year.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [DataRow(2024, true)]
    [DataRow(2026, true)]
    [DataRow(2025, false)]
    [DataRow(2030, false)]
    public void AppliesTo_WhenOnlyYears_ShouldMatchOnlyListedYears(int year, bool expected)
    {
        RuleApplicability applicability = Years(null, null, onlyYears: [2024, 2026]);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.ExceptYears" /> suppresses applicability for the listed years and
    /// applies for every other year.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [DataRow(2025, false)]
    [DataRow(2024, true)]
    [DataRow(2026, true)]
    public void AppliesTo_WhenExceptYears_ShouldSuppressListedYears(int year, bool expected)
    {
        RuleApplicability applicability = Years(null, null, exceptYears: [2025]);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that an <c>ExceptYear</c> inside a <c>fromYear</c>/<c>toYear</c> window is suppressed while the rest of
    /// the window remains applicable.
    /// </summary>
    [TestMethod]
    public void AppliesTo_WhenWindowWithExceptYear_ShouldSuppressOnlyThatYear()
    {
        RuleApplicability applicability = Years(2020, 2030, exceptYears: [2025]);

        Assert.IsTrue(applicability.AppliesTo("XX", 2024));
        Assert.IsFalse(applicability.AppliesTo("XX", 2025));
        Assert.IsTrue(applicability.AppliesTo("XX", 2026));
        Assert.IsFalse(applicability.AppliesTo("XX", 2019));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.AppliesTo" /> throws <see cref="ArgumentNullException" /> when the
    /// territory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppliesTo_WhenTerritoryIsNull_ShouldThrowExactly()
    {
        RuleApplicability applicability = Years(null, null);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = applicability.AppliesTo(null!, 2025);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // MatchesTerritory truth table
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a global rule (no territories) matches every requested territory.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    [TestMethod]
    [DataRow("US")]
    [DataRow("AU")]
    [DataRow("AU-NSW")]
    public void MatchesTerritory_WhenGlobal_ShouldMatchEveryTerritory(string territory)
    {
        Assert.IsTrue(Territories().MatchesTerritory(territory));
    }

    /// <summary>
    /// Verifies that a territory-scoped rule matches an exact code, matches a subnational child of a national scope, and
    /// rejects a national request against a subnational-only scope or an unrelated code.
    /// </summary>
    /// <param name="scope">The single territory the rule is scoped to.</param>
    /// <param name="requested">The requested territory code.</param>
    /// <param name="expected">The expected territory-match outcome.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("AU", "AU", true)]        // exact
    [DataRow("AU", "AU-NSW", true)]    // national scope matches subnational request
    [DataRow("AU", "US", false)]       // unrelated
    [DataRow("AU-NSW", "AU-NSW", true)] // exact subnational
    [DataRow("AU-NSW", "AU", false)]   // subnational scope does not match national request
    [DataRow("AU-NSW", "AU-VIC", false)] // sibling subnational
    [DataRow("AU", "au-nsw", true)]    // case-insensitive territory comparison
    [DataRow("au", "AU-NSW", true)]    // case-insensitive scope
    public void MatchesTerritory_WhenScoped_ShouldMatchExpected(string scope, string requested, bool expected)
    {
        Assert.AreEqual(expected, Territories(scope).MatchesTerritory(requested));
    }

    /// <summary>
    /// Verifies that a rule scoped to several territories matches a request against any one of them.
    /// </summary>
    [TestMethod]
    public void MatchesTerritory_WhenMultipleScopes_ShouldMatchAnyListed()
    {
        RuleApplicability applicability = Territories("US", "AU");

        Assert.IsTrue(applicability.MatchesTerritory("US"));
        Assert.IsTrue(applicability.MatchesTerritory("AU-NSW"));
        Assert.IsFalse(applicability.MatchesTerritory("CA"));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.MatchesTerritory" /> throws <see cref="ArgumentNullException" /> when
    /// the territory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MatchesTerritory_WhenTerritoryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Territories("AU").MatchesTerritory(null!);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // MatchSpecificity truth table
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.MatchSpecificity" /> returns <c>0</c> for a global rule, the matching
    /// scope length for an exact or parent match, and <c>-1</c> when the rule does not match.
    /// </summary>
    /// <param name="scope">The single territory the rule is scoped to, or an empty string for a global rule.</param>
    /// <param name="requested">The requested territory code.</param>
    /// <param name="expected">The expected specificity.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "AU", 0)]          // global
    [DataRow("", "AU-NSW", 0)]      // global
    [DataRow("AU", "AU", 2)]        // exact national
    [DataRow("AU", "AU-NSW", 2)]    // parent of subnational
    [DataRow("AU-NSW", "AU-NSW", 6)] // exact subnational
    [DataRow("AU", "US", -1)]       // no match
    [DataRow("AU-NSW", "AU", -1)]   // subnational scope, national request
    public void MatchSpecificity_WhenScoped_ShouldReturnExpected(string scope, string requested, int expected)
    {
        RuleApplicability applicability = scope.Length == 0 ? Territories() : Territories(scope);

        Assert.AreEqual(expected, applicability.MatchSpecificity(requested));
    }

    /// <summary>
    /// Verifies that a narrower scope yields a higher specificity than a broader scope for the same subnational request,
    /// which is the ordering that lets a subdivision rule shadow a national rule.
    /// </summary>
    [TestMethod]
    public void MatchSpecificity_WhenNarrowerScope_ShouldExceedBroaderScope()
    {
        int national = Territories("AU").MatchSpecificity("AU-NSW");
        int subnational = Territories("AU-NSW").MatchSpecificity("AU-NSW");

        Assert.IsTrue(subnational > national);
    }

    // -----------------------------------------------------------------------------------------------------------
    // End-to-end applicability via the service
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a service over the applicability fixture.
    /// </summary>
    /// <returns>A service for the fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("applicability.xml");

    /// <summary>
    /// Resolves the supplied territory and year and reports whether the supplied concept id is emitted.
    /// </summary>
    /// <param name="notableDateId">The concept id to look for.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <returns><see langword="true" /> if the concept is emitted; otherwise <see langword="false" />.</returns>
    private static bool Emits(string notableDateId, string territory, int year)
    {
        DateRange range = new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

        return CreateService().Resolve(range, territory).Any(r => r.NotableDateId == notableDateId);
    }

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
    /// Verifies that <c>OnlyYear</c> and <c>ExceptYear</c> rules are emitted exactly for the listed and unlisted years
    /// when resolved through the service.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOnlyAndExceptYears_EmitsAccordingToSets()
    {
        Assert.IsTrue(Emits("only-years", "XX", 2024));
        Assert.IsFalse(Emits("only-years", "XX", 2025));
        Assert.IsTrue(Emits("only-years", "XX", 2026));

        Assert.IsTrue(Emits("except-2025", "XX", 2024));
        Assert.IsFalse(Emits("except-2025", "XX", 2025));

        Assert.IsTrue(Emits("windowed-except", "XX", 2024));
        Assert.IsFalse(Emits("windowed-except", "XX", 2025));
        Assert.IsFalse(Emits("windowed-except", "XX", 2031));
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
    [TestMethod]
    public void Resolve_WhenSubnationalRule_EmitsOnlyForSubdivision()
    {
        Assert.IsTrue(Emits("subnational-nsw", "AU-NSW", 2026));
        Assert.IsFalse(Emits("subnational-nsw", "AU", 2026));
    }

    /// <summary>
    /// Verifies that when a concept carries both a national and a narrower subdivision rule, a subdivision request emits
    /// only the narrower rule (it shadows the national one) while any other subdivision falls back to the national rule.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTerritoryShadowing_NarrowerRuleShadowsNationalRule()
    {
        DateRange range = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        NotableDateService service = CreateService();

        NotableDate nsw = service.Resolve(range, "AU-NSW").Single(r => r.NotableDateId == "shadowed");
        Assert.AreEqual("nsw", nsw.RuleId);

        NotableDate vic = service.Resolve(range, "AU-VIC").Single(r => r.NotableDateId == "shadowed");
        Assert.AreEqual("national", vic.RuleId);
    }
}

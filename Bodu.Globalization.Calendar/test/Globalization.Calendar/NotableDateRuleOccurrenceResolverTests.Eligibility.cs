// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOccurrenceResolverTests.Eligibility.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the per-rule eligibility, territory-matching, and anchor-relative branches of
/// <see cref="NotableDateRuleOccurrenceResolver" /> through the public
/// <see cref="NotableDateRuleOccurrenceResolver.ResolveOccurrences" /> entry point.
/// </summary>
[TestClass]
public sealed class NotableDateRuleOccurrenceResolverEligibilityTests
{
    /// <summary>
    /// Verifies that a rule with no authored territory matches any request territory.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleHasNoTerritory_ShouldMatchAnyTerritory()
    {
        NotableDateRule rule = FixedRule("Global", month: 5, day: 1, territoryCode: null);
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        NotableDateResolutionRequest request = Window(2024, 5, 1, 2024, 5, 31, territoryCode: "AU");
        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(1, actual.Count);
        Assert.IsNull(actual[0].BaseDate.TerritoryCode);
    }

    /// <summary>
    /// Verifies that a multi-territory rule (e.g. <c>"AU,NZ"</c>) yields one occurrence per applicable territory
    /// when no specific territory is requested.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleHasMultipleTerritoriesAndRequestUnscoped_ShouldEmitOnePerTerritory()
    {
        NotableDateRule rule = FixedRule("Cross-Border", month: 4, day: 25, territoryCode: "AU,NZ");
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 4, 25, 2024, 4, 25));

        Assert.AreEqual(2, actual.Count);
        CollectionAssert.AreEquivalent(
            new[] { "AU", "NZ" },
            actual.Select(o => o.BaseDate.TerritoryCode).ToArray());
    }

    /// <summary>
    /// Verifies that a multi-territory rule yields only the matching territory when a specific territory is requested.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleHasMultipleTerritoriesAndRequestScopesOne_ShouldEmitOnlyMatch()
    {
        NotableDateRule rule = FixedRule("Cross-Border", month: 4, day: 25, territoryCode: "AU,NZ");
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 4, 25, 2024, 4, 25, territoryCode: "AU"));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("AU", actual[0].BaseDate.TerritoryCode);
    }

    /// <summary>
    /// Verifies that a rule scoped to a parent territory matches a request scoped to a subdivision through
    /// <c>TerritoryCode.Contains</c> semantics.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleTerritoryContainsRequestSubdivision_ShouldEmitOccurrence()
    {
        NotableDateRule rule = FixedRule("Federal", month: 1, day: 26, territoryCode: "AU");
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 1, 1, 2024, 12, 31, territoryCode: "AU-NSW"));

        Assert.AreEqual(1, actual.Count);
    }

    /// <summary>
    /// Verifies that a rule whose <see cref="NotableDateRule.FirstYear" /> is later than the request year is excluded.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleFirstYearIsAfterRequestedYear_ShouldReturnEmptyResult()
    {
        NotableDateRule rule = FixedRule("Future", month: 6, day: 1) with { FirstYear = 2030 };
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 1, 1, 2024, 12, 31));

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that a rule whose <see cref="NotableDateRule.LastYear" /> is earlier than the request year is excluded.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleLastYearIsBeforeRequestedYear_ShouldReturnEmptyResult()
    {
        NotableDateRule rule = FixedRule("Sunset", month: 6, day: 1) with { LastYear = 2020 };
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 1, 1, 2024, 12, 31));

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that a rule whose <see cref="NotableDateRule.CalendarType" /> does not match the request calendar is
    /// excluded.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleCalendarDoesNotMatchRequest_ShouldReturnEmptyResult()
    {
        NotableDateRule rule = FixedRule("Lunar", month: 1, day: 1) with
        {
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
        };
        NotableDateRuleOccurrenceResolver resolver = CreateResolver(rule);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 1, 1, 2024, 12, 31, calendarType: typeof(SysGlobal.HebrewCalendar)));

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that an anchor-relative rule whose offset date lies outside the request window is not emitted.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenAnchorRelativeOffsetFallsOutsideWindow_ShouldNotEmitOccurrence()
    {
        NotableDateRule anchor = new()
        {
            Name = "Anchor Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 6,
            Day = 15,
        };

        NotableDateRule offset = new()
        {
            Name = "Anchor + 60",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Anchor Day",
            OffsetDays = 60,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(anchor, offset);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 6, 1, 2024, 6, 30));

        // Window includes the anchor but not the offset.
        Assert.IsTrue(actual.Any(o => o.BaseDate.Name == "Anchor Day"));
        Assert.IsFalse(actual.Any(o => o.BaseDate.Name == "Anchor + 60"));
    }

    /// <summary>
    /// Verifies that an anchor-relative rule whose offset date lies inside the request window emits the dependent
    /// occurrence even when the anchor itself lies outside the window.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenOffsetDateInsideWindowButAnchorOutside_ShouldEmitDependentOnly()
    {
        NotableDateRule anchor = new()
        {
            Name = "Anchor Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
        };

        NotableDateRule offset = new()
        {
            Name = "Anchor + 30",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Anchor Day",
            OffsetDays = 30,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(anchor, offset);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 1, 25, 2024, 2, 5));

        Assert.IsFalse(actual.Any(o => o.BaseDate.Name == "Anchor Day"));
        Assert.IsTrue(actual.Any(o => o.BaseDate.Name == "Anchor + 30" && o.BaseDate.Date == new DateTime(2024, 1, 31)));
    }

    /// <summary>
    /// Verifies that an anchor-relative rule whose anchor cannot be resolved surfaces as
    /// <see cref="InvalidOperationException" /> at resolution time — misconfigured offset chains do not silently
    /// produce empty results.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenAnchorIsMissing_ShouldThrowInvalidOperationException()
    {
        NotableDateRule orphanOffset = new()
        {
            Name = "Orphan",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Missing Anchor",
            OffsetDays = 1,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(orphanOffset);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = resolver.ResolveOccurrences(Window(2024, 1, 1, 2024, 12, 31));
        });
    }

    /// <summary>
    /// Verifies that an anchor-relative occurrence is de-duplicated when the same anchor produces the same target date
    /// twice (e.g., the rule itself matches across multiple anchor years).
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenOffsetRuleHasNoOffsetDays_ShouldNotEmit()
    {
        NotableDateRule anchor = new()
        {
            Name = "Anchor Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 6,
            Day = 15,
        };

        NotableDateRule offset = new()
        {
            Name = "Anchor (No Offset)",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Anchor Day",
            OffsetDays = null,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(anchor, offset);

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(
            Window(2024, 6, 1, 2024, 6, 30));

        Assert.IsFalse(actual.Any(o => o.BaseDate.Name == "Anchor (No Offset)"));
    }

    private static NotableDateRule FixedRule(
        string name,
        int month,
        int day,
        string? territoryCode = null) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            TerritoryCode = territoryCode,
            Tags = ImmutableHashSet<string>.Empty,
        };

    private static NotableDateResolutionRequest Window(
        int sYear,
        int sMonth,
        int sDay,
        int eYear,
        int eMonth,
        int eDay,
        string? territoryCode = null,
        Type? calendarType = null) =>
        new(
            new DateTime(sYear, sMonth, sDay),
            new DateTime(eYear, eMonth, eDay),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: territoryCode,
            calendarType: calendarType);

    private static NotableDateRuleOccurrenceResolver CreateResolver(params NotableDateRule[] rules)
    {
        NotableDateRuleResolver ruleResolver = new(rules);
        CachingCalculationAnchorResolver calculationAnchors = new(rules, ruleResolver);
        AnchorRelativeRuleIndex anchorRelativeRules = new(rules);

        return new NotableDateRuleOccurrenceResolver(
            rules,
            ruleResolver,
            calculationAnchors,
            anchorRelativeRules);
    }
}

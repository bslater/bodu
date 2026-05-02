// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOccurrenceResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateRuleOccurrenceResolver" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateRuleOccurrenceResolverTests
{
    /// <summary>
    /// Verifies that a requested window containing Start of Lent resolves the Easter anchor without emitting Easter Sunday.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenWindowContainsStartOfLentButNotEaster_ShouldEmitStartOfLentOnly()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateRule easterSunday = EasterSundayRule();
        NotableDateRule startOfLent = OffsetRule("Start of Lent", offsetDays: -46);
        NotableDateRule palmSunday = OffsetRule("Palm Sunday", offsetDays: -7);
        NotableDateRule goodFriday = OffsetRule("Good Friday", offsetDays: -2);

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(
            easterSunday,
            startOfLent,
            palmSunday,
            goodFriday);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Start of Lent", actual[0].BaseDate.Name);
        Assert.AreEqual(new DateTime(2024, 2, 14), actual[0].BaseDate.Date);

        Assert.IsFalse(actual.Any(occurrence => occurrence.BaseDate.Name == "Easter Sunday"));
        Assert.IsFalse(actual.Any(occurrence => occurrence.BaseDate.Name == "Palm Sunday"));
        Assert.IsFalse(actual.Any(occurrence => occurrence.BaseDate.Name == "Good Friday"));

        Assert.AreEqual(3, CountingEasterAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that the same cached calculation anchor is reused when a later request resolves Palm Sunday.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenLaterWindowUsesSameAnchorYear_ShouldReuseCachedAnchor()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateRule easterSunday = EasterSundayRule();
        NotableDateRule startOfLent = OffsetRule("Start of Lent", offsetDays: -46);
        NotableDateRule palmSunday = OffsetRule("Palm Sunday", offsetDays: -7);

        NotableDateRule[] rules =
        [
            easterSunday,
            startOfLent,
            palmSunday,
        ];

        NotableDateRuleResolver ruleResolver = new(rules);
        CachingCalculationAnchorResolver calculationAnchors = new(rules, ruleResolver);
        AnchorRelativeRuleIndex anchorRelativeRules = new(rules);

        NotableDateRuleOccurrenceResolver resolver = new(
            rules,
            ruleResolver,
            calculationAnchors,
            anchorRelativeRules);

        NotableDateResolutionRequest lentRequest = new(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        NotableDateResolutionRequest palmSundayRequest = new(
            new DateTime(2024, 3, 20),
            new DateTime(2024, 3, 25),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<ResolvedNotableDateOccurrence> lent = resolver.ResolveOccurrences(lentRequest);
        IReadOnlyList<ResolvedNotableDateOccurrence> palmSundayOccurrences = resolver.ResolveOccurrences(palmSundayRequest);

        Assert.AreEqual(1, lent.Count);
        Assert.AreEqual("Start of Lent", lent[0].BaseDate.Name);
        Assert.AreEqual(new DateTime(2024, 2, 14), lent[0].BaseDate.Date);

        Assert.AreEqual(1, palmSundayOccurrences.Count);
        Assert.AreEqual("Palm Sunday", palmSundayOccurrences[0].BaseDate.Name);
        Assert.AreEqual(new DateTime(2024, 3, 24), palmSundayOccurrences[0].BaseDate.Date);

        // Candidate years are conservative: 2023, 2024, 2025.
        // The second request reuses those same anchor-year calculations instead of invoking the algorithm again.
        Assert.AreEqual(3, CountingEasterAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that a window containing Easter Sunday emits Easter Sunday when the Easter rule itself falls inside the window.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenWindowContainsEasterSunday_ShouldEmitEasterSunday()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateRule easterSunday = EasterSundayRule();
        NotableDateRule startOfLent = OffsetRule("Start of Lent", offsetDays: -46);

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(
            easterSunday,
            startOfLent);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 3, 30),
            new DateTime(2024, 4, 1),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Easter Sunday", actual[0].BaseDate.Name);
        Assert.AreEqual(new DateTime(2024, 3, 31), actual[0].BaseDate.Date);
    }

    /// <summary>
    /// Verifies that date-level filters are applied after materialisation.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenDateLevelFilterExcludesOccurrence_ShouldReturnEmptyResult()
    {
        NotableDateRule easterSunday = EasterSundayRule();

        NotableDateRule startOfLent = OffsetRule("Start of Lent", offsetDays: -46) with
        {
            DurationDays = 1,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(
            easterSunday,
            startOfLent);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.WithMinDuration(2));

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that rule-level filters are applied before direct rule materialisation.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleLevelFilterExcludesCategory_ShouldReturnEmptyResult()
    {
        NotableDateRule easterSunday = EasterSundayRule() with
        {
            Category = NotableDateCategory.Religious,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(easterSunday);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 3, 30),
            new DateTime(2024, 4, 1),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Cultural));

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that territory-scoped rules are expanded only when compatible with the requested territory.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleTerritoryMatchesRequestedTerritory_ShouldEmitOccurrence()
    {
        NotableDateRule nswRule = FixedRule("NSW Observance", month: 8, day: 5) with
        {
            TerritoryCode = "AU-NSW",
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(nswRule);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 8, 1),
            new DateTime(2024, 8, 10),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: "AU");

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("AU-NSW", actual[0].BaseDate.TerritoryCode);
    }

    /// <summary>
    /// Verifies that territory-scoped rules are excluded when incompatible with the requested territory.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenRuleTerritoryDoesNotMatchRequestedTerritory_ShouldReturnEmptyResult()
    {
        NotableDateRule nswRule = FixedRule("NSW Observance", month: 8, day: 5) with
        {
            TerritoryCode = "AU-NSW",
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(nswRule);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 8, 1),
            new DateTime(2024, 8, 10),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: "NZ");

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that multi-day occurrences are included when their span intersects the requested window.
    /// </summary>
    [TestMethod]
    public void ResolveOccurrences_WhenMultiDayOccurrenceIntersectsWindow_ShouldEmitOccurrence()
    {
        NotableDateRule festival = FixedRule("Religious Festival", month: 6, day: 10) with
        {
            DurationDays = 5,
        };

        NotableDateRuleOccurrenceResolver resolver = CreateResolver(festival);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<ResolvedNotableDateOccurrence> actual = resolver.ResolveOccurrences(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Religious Festival", actual[0].BaseDate.Name);
        Assert.AreEqual(new DateTime(2024, 6, 10), actual[0].BaseDate.Date);
        Assert.AreEqual(new DateTime(2024, 6, 14), actual[0].BaseDate.EndDate);
    }

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

    private static NotableDateRule EasterSundayRule() =>
        new()
        {
            Name = "Easter Sunday",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmType = typeof(CountingEasterAlgorithm),
            IsNonWorkingDay = false,
            Tags = ImmutableHashSet.Create("Christian"),
        };

    private static NotableDateRule OffsetRule(string name, int offsetDays) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            AnchorRuleName = "Easter Sunday",
            OffsetDays = offsetDays,
            IsNonWorkingDay = false,
            Tags = ImmutableHashSet.Create("Christian"),
        };

    private static NotableDateRule FixedRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Religious,
            Month = month,
            Day = day,
            IsNonWorkingDay = false,
        };

    private sealed class CountingEasterAlgorithm : INotableDateAlgorithm
    {
        public static int CallCount { get; private set; }

        public static void Reset() => CallCount = 0;

        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
        {
            CallCount++;

            return year switch
            {
                2023 => new DateTime(2023, 4, 9),
                2024 => new DateTime(2024, 3, 31),
                2025 => new DateTime(2025, 4, 20),
                _ => null,
            };
        }
    }
}
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.FilterCombinators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Data-driven tests for every <see cref="NotableDateFilter" /> factory method and combinator (<see cref="NotableDateFilter.And" />,
/// <see cref="NotableDateFilter.Or" />, <see cref="NotableDateFilter.AllOf" />, <see cref="NotableDateFilter.AnyOf" />), exercised
/// end-to-end through <see cref="NotableDateService.ResolveNotableDatesInRange" />.
/// </summary>
/// <remarks>
/// <para>
/// All tests share a single fixture rule set covering distinct combinations of category, tag, name, duration, non-working flag,
/// and observance adjustment so each filter can demonstrably select / reject specific rules without resorting to one rule per
/// test.
/// </para>
/// </remarks>
public sealed partial class NotableDateRangePipelineScenarioTests
{
    // =====================================================================================================================
    // Single-factory filter tests
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> emits only rules whose <see cref="NotableDateCategory" />
    /// matches the requested category.
    /// </summary>
    [TestMethod]
    [DataRow("Holiday", "Christmas Day,Boxing Day,Australia Day,Year-End Holiday")]
    [DataRow("Religious", "Hanukkah")]
    [DataRow("Cultural", "Lunar Festival")]
    [DataRow("Civic", "Labour Day")]
    [DataRow("Bank", "Bank Holiday")]
    [DataRow("Seasonal", "")]
    public void Filter_ForCategory_ShouldOnlyEmitRulesOfMatchingCategory(string categoryName, string expectedNamesCsv)
    {
        var category = (NotableDateCategory)Enum.Parse(typeof(NotableDateCategory), categoryName);
        var filter = NotableDateFilter.ForCategory(category);

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, ParseExpectedNames(expectedNamesCsv));
        Assert.IsTrue(resolved.All(n => n.Category == category),
            $"All emitted dates must belong to {category}.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> emits rules whose category is any of the listed categories.
    /// </summary>
    [TestMethod]
    public void Filter_ForAnyCategory_ShouldEmitRulesInAnyListedCategory()
    {
        var filter = NotableDateFilter.ForAnyCategory(
            NotableDateCategory.Religious,
            NotableDateCategory.Cultural);

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Hanukkah", "Lunar Festival");
        Assert.IsTrue(resolved.All(n => n.Category == NotableDateCategory.Religious || n.Category == NotableDateCategory.Cultural));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> emits rules whose tag set contains the supplied tag
    /// (case-insensitive).
    /// </summary>
    [TestMethod]
    [DataRow("Christian", "Christmas Day,Boxing Day")]
    [DataRow("Public", "Christmas Day,Boxing Day,Australia Day,Bank Holiday,Year-End Holiday")]
    [DataRow("CHRISTIAN", "Christmas Day,Boxing Day")] // case-insensitive
    [DataRow("Jewish", "Hanukkah")]
    [DataRow("Workers", "Labour Day")]
    [DataRow("Asian", "Lunar Festival")]
    [DataRow("Nonexistent", "")]
    public void Filter_WithTag_ShouldEmitRulesContainingTagCaseInsensitive(string tag, string expectedNamesCsv)
    {
        var filter = NotableDateFilter.WithTag(tag);

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, ParseExpectedNames(expectedNamesCsv));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> emits rules whose tag set intersects the supplied tag list.
    /// </summary>
    [TestMethod]
    public void Filter_WithAnyTag_ShouldEmitRulesContainingAnyOfTheTags()
    {
        var filter = NotableDateFilter.WithAnyTag("Christian", "Jewish");

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day", "Hanukkah");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> emits only rules whose tag set contains every supplied tag.
    /// </summary>
    [TestMethod]
    public void Filter_WithAllTags_ShouldOnlyEmitRulesContainingEveryTag()
    {
        var filter = NotableDateFilter.WithAllTags("Christian", "Public");

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> emits only rules whose name matches case-insensitively.
    /// </summary>
    [TestMethod]
    [DataRow("Christmas Day", "Christmas Day")]
    [DataRow("CHRISTMAS DAY", "Christmas Day")]
    [DataRow("christmas day", "Christmas Day")]
    [DataRow("Hanukkah", "Hanukkah")]
    [DataRow("Does Not Exist", "")]
    public void Filter_WithName_ShouldEmitRulesByNameCaseInsensitive(string name, string expectedNamesCsv)
    {
        var filter = NotableDateFilter.WithName(name);

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, ParseExpectedNames(expectedNamesCsv));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> emits rules whose name appears in the supplied set.
    /// </summary>
    [TestMethod]
    public void Filter_WithAnyName_ShouldEmitRulesInListedNameSet()
    {
        var filter = NotableDateFilter.WithAnyName("Hanukkah", "Labour Day", "Bank Holiday");

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Hanukkah", "Labour Day", "Bank Holiday");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> emits only rules flagged as non-working — including
    /// rules whose adjustments declare non-working substitutes.
    /// </summary>
    [TestMethod]
    public void Filter_IsNonWorkingDay_ShouldEmitOnlyNonWorkingRules()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day", "Australia Day", "Labour Day", "Bank Holiday", "Year-End Holiday");
        Assert.IsTrue(resolved.All(n => n.IsNonWorkingDay));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> emits only rules whose
    /// <see cref="NotableDateRule.DurationDays" /> meets or exceeds the supplied minimum.
    /// </summary>
    [TestMethod]
    [DataRow(1, "Christmas Day,Boxing Day,Lunar Festival,Hanukkah,Australia Day,Labour Day,Bank Holiday,Year-End Holiday")]
    [DataRow(2, "Lunar Festival,Hanukkah")]
    [DataRow(7, "Lunar Festival,Hanukkah")]
    [DataRow(8, "Hanukkah")]
    [DataRow(9, "")]
    public void Filter_WithMinDuration_ShouldEmitOnlyRulesAtOrAboveDuration(int minimumDays, string expectedNamesCsv)
    {
        var filter = NotableDateFilter.WithMinDuration(minimumDays);

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, ParseExpectedNames(expectedNamesCsv));
    }

    // =====================================================================================================================
    // Secondary-gate filters (date-level)
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> applies a date-level cut on top of the request window so only
    /// observed dates that intersect the supplied inclusive range are emitted.
    /// </summary>
    [TestMethod]
    public void Filter_InDateRange_ShouldRestrictByDateRangeSecondaryGate()
    {
        // Restrict to Christmas week 2026 — Christmas Day (25 Dec) and Boxing Day (26 Dec) intersect, others do not.
        var filter = NotableDateFilter.InDateRange(
            new DateTime(2026, 12, 24),
            new DateTime(2026, 12, 27));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter, queryYear: 2026);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> emits only occurrences whose <c>WasAdjusted</c> flag is
    /// <see langword="true" /> — that is, occurrences shifted by an observance adjustment.
    /// </summary>
    [TestMethod]
    public void Filter_WasAdjusted_ShouldOnlyEmitOccurrencesShiftedByAdjustment()
    {
        // 31 Dec 2022 = Saturday. The Year-End Holiday rule's IfWeekend / MoveToNextNonWorkingDay adjustment fires and lands on
        // Mon 2 Jan 2023 (no blocker on Mon since Hanukkah 2022 is in early Dec, not at Jan boundary).
        var filter = NotableDateFilter.WasAdjusted();

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(
            filter,
            windowStart: new DateTime(2022, 12, 25),
            windowEnd: new DateTime(2023, 1, 5));

        Assert.IsTrue(resolved.All(n => n.WasAdjusted),
            $"Every emitted date must carry an AdjustmentReason; got: {string.Join(", ", resolved.Select(n => $"{n.Name}@{n.Date:yyyy-MM-dd} adjusted={n.WasAdjusted}"))}.");

        Assert.IsTrue(resolved.Any(n => n.Name == "Year-End Holiday" && n.Date == new DateTime(2023, 1, 2)),
            "The rolled-forward Year-End Holiday on 2 Jan 2023 must pass the WasAdjusted filter.");
    }

    // =====================================================================================================================
    // Combinators
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> requires both sub-filters to pass — only rules satisfying both gates
    /// are emitted.
    /// </summary>
    [TestMethod]
    public void Combinator_And_ShouldEmitOnlyRulesPassingBothFilters()
    {
        NotableDateFilter filter = NotableDateFilter
            .ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Christian"));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> emits the union of two sub-filters — rules passing either gate.
    /// </summary>
    [TestMethod]
    public void Combinator_Or_ShouldEmitUnionOfBothFilters()
    {
        NotableDateFilter filter = NotableDateFilter
            .ForCategory(NotableDateCategory.Religious)
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Civic));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Hanukkah", "Labour Day");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> requires every supplied filter to pass — equivalent to chained
    /// <see cref="NotableDateFilter.And" />.
    /// </summary>
    [TestMethod]
    public void Combinator_AllOf_ShouldRequireEveryFilterToPass()
    {
        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.IsNonWorkingDay(),
            NotableDateFilter.WithTag("Public"));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day", "Australia Day", "Year-End Holiday");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> emits the union across every supplied filter — equivalent to chained
    /// <see cref="NotableDateFilter.Or" />.
    /// </summary>
    [TestMethod]
    public void Combinator_AnyOf_ShouldEmitUnionAcrossEveryFilter()
    {
        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.WithTag("Workers"),
            NotableDateFilter.WithTag("Jewish"),
            NotableDateFilter.WithTag("Asian"));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Labour Day", "Hanukkah", "Lunar Festival");
    }

    /// <summary>
    /// Verifies that nested combinators <c>(A AND B) OR C</c> evaluate correctly, exercising both gate combinations.
    /// </summary>
    [TestMethod]
    public void Combinator_Nested_AndOrCombination_ShouldEvaluateCorrectly()
    {
        // (Holiday AND Christian) OR Religious
        // → Christmas Day + Boxing Day (Holiday + Christian) ∪ Hanukkah (Religious)
        NotableDateFilter filter = NotableDateFilter
            .ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Christian"))
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day", "Hanukkah");
    }

    /// <summary>
    /// Verifies that an <see cref="NotableDateFilter.And" /> of two filters with no common rules emits nothing.
    /// </summary>
    [TestMethod]
    public void Combinator_DisjointAnd_ShouldEmitNothing()
    {
        // Holiday AND Jewish — no rule satisfies both (Hanukkah is Religious, not Holiday).
        NotableDateFilter filter = NotableDateFilter
            .ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.WithTag("Jewish"));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter);

        Assert.AreEqual(0, resolved.Count,
            $"Disjoint AND should emit nothing; got: {string.Join(", ", resolved.Select(n => n.Name))}.");
    }

    /// <summary>
    /// Verifies that combining a primary-gate filter with a secondary-gate filter via <see cref="NotableDateFilter.And" />
    /// applies both: the primary skips rule resolution for non-matching rules and the secondary trims by date.
    /// </summary>
    [TestMethod]
    public void Combinator_PrimaryAndSecondaryGate_ShouldApplyBothStages()
    {
        // Holiday AND InDateRange(24..27 Dec 2026)
        // Primary skips Hanukkah, Lunar Festival, Labour Day, Bank Holiday (non-Holiday categories).
        // Secondary then filters Christmas (Dec 25) ✓, Boxing (Dec 26) ✓, Australia Day (Jan 26) ✗, Year-End (Dec 31) ✗.
        NotableDateFilter filter = NotableDateFilter
            .ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.InDateRange(new DateTime(2026, 12, 24), new DateTime(2026, 12, 27)));

        IReadOnlyList<NotableDate> resolved = ResolveFilterFixture(filter, queryYear: 2026);

        AssertEmittedNames(resolved, "Christmas Day", "Boxing Day");
    }

    /// <summary>
    /// Verifies that an <see cref="NotableDateFilter.AnyOf" /> with a single filter behaves identically to that filter alone.
    /// </summary>
    [TestMethod]
    public void Combinator_AnyOf_WithSingleFilter_ShouldDelegateToThatFilter()
    {
        var standalone = NotableDateFilter.ForCategory(NotableDateCategory.Bank);
        var wrapped = NotableDateFilter.AnyOf(NotableDateFilter.ForCategory(NotableDateCategory.Bank));

        IReadOnlyList<NotableDate> standaloneResult = ResolveFilterFixture(standalone);
        IReadOnlyList<NotableDate> wrappedResult = ResolveFilterFixture(wrapped);

        CollectionAssert.AreEqual(
            standaloneResult.Select(n => n.Name).OrderBy(s => s).ToArray(),
            wrappedResult.Select(n => n.Name).OrderBy(s => s).ToArray());
    }

    // =====================================================================================================================
    // Validation
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> rejects an empty parameter array.
    /// </summary>
    [TestMethod]
    public void Filter_ForAnyCategory_WhenCalledWithNoCategories_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.ForAnyCategory();
        });

        Assert.AreEqual("categories", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> rejects whitespace-only input.
    /// </summary>
    [TestMethod]
    public void Filter_WithTag_WhenCalledWithWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateFilter.WithTag("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> rejects an inverted range (end before start).
    /// </summary>
    [TestMethod]
    public void Filter_InDateRange_WhenEndIsBeforeStart_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFilter.InDateRange(new DateTime(2026, 12, 31), new DateTime(2026, 1, 1));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> rejects values less than one.
    /// </summary>
    [TestMethod]
    public void Filter_WithMinDuration_WhenLessThanOne_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFilter.WithMinDuration(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> rejects a <see langword="null" /> sibling.
    /// </summary>
    [TestMethod]
    public void Combinator_And_WhenOtherIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.ForCategory(NotableDateCategory.Holiday).And(null!);
        });
    }

    // =====================================================================================================================
    // Helpers — fixture rule set, runner, and assertion utilities
    // =====================================================================================================================

    /// <summary>
    /// Resolves the fixture rule set against the supplied filter using a year-wide window for the supplied <paramref name="queryYear" />.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="queryYear">The civil year to query (defaults to 2026).</param>
    /// <returns>The resolved notable dates.</returns>
    private static IReadOnlyList<NotableDate> ResolveFilterFixture(NotableDateFilter filter, int queryYear = 2026) =>
        ResolveFilterFixture(
            filter,
            new DateTime(queryYear, 1, 1),
            new DateTime(queryYear, 12, 31));

    /// <summary>
    /// Resolves the fixture rule set against the supplied filter and explicit window.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="windowStart">The inclusive start of the request window.</param>
    /// <param name="windowEnd">The inclusive end of the request window.</param>
    /// <returns>The resolved notable dates.</returns>
    private static IReadOnlyList<NotableDate> ResolveFilterFixture(NotableDateFilter filter, DateTime windowStart, DateTime windowEnd)
    {
        NotableDateService service = BuildService(FilterFixtureRules());
        return service.ResolveNotableDatesInRange(windowStart, windowEnd, filter: filter);
    }

    /// <summary>
    /// Asserts that the emitted set's distinct names equal the supplied expected set, regardless of order.
    /// </summary>
    /// <param name="resolved">The resolved notable dates.</param>
    /// <param name="expectedNames">The expected distinct names.</param>
    private static void AssertEmittedNames(IReadOnlyList<NotableDate> resolved, params string[] expectedNames)
    {
        var actualNames = resolved.Select(n => n.Name).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
        var expectedSorted = expectedNames.OrderBy(s => s, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(
            expectedSorted,
            actualNames,
            $"Expected: [{string.Join(", ", expectedSorted)}]; got: [{string.Join(", ", actualNames)}].");
    }

    /// <summary>
    /// Splits a comma-separated expected-name list, returning an empty array when the input is empty or whitespace.
    /// </summary>
    /// <param name="csv">The comma-separated list.</param>
    /// <returns>The trimmed name array.</returns>
    private static string[] ParseExpectedNames(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Builds the diverse fixture rule set used by every filter test. Each rule occupies a distinct combination of category,
    /// tag set, name, duration, and non-working flag so individual filters can be observed selecting precise subsets.
    /// </summary>
    /// <returns>The fixture rules.</returns>
    private static NotableDateRule[] FilterFixtureRules() =>
    [
        new NotableDateRule
        {
            Name = "Christmas Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 25,
            IsNonWorkingDay = true,
            Tags = ["Christian", "Public"],
        },
        new NotableDateRule
        {
            Name = "Boxing Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 26,
            IsNonWorkingDay = true,
            Tags = ["Christian", "Public"],
        },
        new NotableDateRule
        {
            Name = "Australia Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 26,
            IsNonWorkingDay = true,
            Tags = ["Public", "Federal"],
        },
        new NotableDateRule
        {
            Name = "Hanukkah",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Religious,
            Month = 12,
            Day = 7,
            IsNonWorkingDay = false,
            DurationDays = 8,
            Tags = ["Jewish"],
        },
        new NotableDateRule
        {
            Name = "Lunar Festival",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 2,
            Day = 17,
            IsNonWorkingDay = false,
            DurationDays = 7,
            Tags = ["Asian", "Festive"],
        },
        new NotableDateRule
        {
            Name = "Labour Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Civic,
            Month = 5,
            Day = 1,
            IsNonWorkingDay = true,
            Tags = ["Workers"],
        },
        new NotableDateRule
        {
            Name = "Bank Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Bank,
            Month = 8,
            Day = 26,
            IsNonWorkingDay = true,
            Tags = ["Public"],
        },
        new NotableDateRule
        {
            Name = "Year-End Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 31,
            IsNonWorkingDay = true,
            Tags = ["Public"],
            Adjustments = [new ObservanceAdjustment
            {
                Key = "weekend-substitute",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
                IsNonWorkingDay = true,
            }],
        },
    ];
}

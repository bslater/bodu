// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.PipelineEquivalence.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Captures the equivalence (or documented divergence) between the legacy year-cache-backed
/// <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" /> family and the newer
/// <see cref="NotableDateService.ResolveNotableDatesInRange" /> pipeline. The suite pins current
/// behaviour ahead of the architectural consolidation that routes the legacy overloads through
/// the pipeline; cases that fail today against the legacy engine are marked
/// <see cref="IgnoreAttribute" /> with a forward-reference comment.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    private static IReadOnlyList<EquivalenceKey> ToKeys(IEnumerable<NotableDate> notables) =>
        notables
            .Select(n => new EquivalenceKey(n.Date, n.EndDate, n.Name, n.TerritoryCode, n.WasAdjusted))
            .OrderBy(k => k.Date)
            .ThenBy(k => k.Name, StringComparer.Ordinal)
            .ThenBy(k => k.TerritoryCode ?? string.Empty, StringComparer.Ordinal)
            .ToList();

    private static void AssertEquivalent(
        IReadOnlyList<NotableDate> legacy,
        IReadOnlyList<NotableDate> pipeline)
    {
        IReadOnlyList<EquivalenceKey> legacyKeys = ToKeys(legacy);
        IReadOnlyList<EquivalenceKey> pipelineKeys = ToKeys(pipeline);

        CollectionAssert.AreEqual(
            legacyKeys.ToList(),
            pipelineKeys.ToList(),
            $"Legacy produced {legacyKeys.Count} entries; pipeline produced {pipelineKeys.Count}.\n" +
            $"Legacy:   {string.Join(", ", legacyKeys)}\n" +
            $"Pipeline: {string.Join(", ", pipelineKeys)}");
    }

    /// <summary>
    /// Verifies that querying a single calendar day yields identical results on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenSingleDayInYear_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1, nonWorking: true),
            Fixed("Christmas Day", 12, 25, nonWorking: true));

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(new DateTime(2026, 1, 1));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a single-month window yields identical results on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenSingleCalendarMonth_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Christmas Day", 12, 25),
            Fixed("Boxing Day", 12, 26));

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a full-year query (<c>GetNotableDates(year)</c>) matches the equivalent
    /// <c>Jan 1 → Dec 31</c> pipeline window.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenFullYearWindow_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1, nonWorking: true),
            Fixed("Labour Day", 5, 1, nonWorking: true),
            Fixed("Christmas Day", 12, 25, nonWorking: true));

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026);
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a multi-year window matches across both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenMultiYearWindow_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1, nonWorking: true),
            Fixed("Christmas Day", 12, 25, nonWorking: true));

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2025, 1, 1), new DateTime(2027, 12, 31));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2025, 1, 1), new DateTime(2027, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that querying a January day for a multi-day span anchored in the previous December
    /// returns the wrap-around occurrence on both paths. The legacy <c>(date)</c> overload checks
    /// <c>[year-1, year]</c> explicitly, so this case is expected to match.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenDecJanCrossoverSingleDay_ShouldMatch()
    {
        NotableDateRule festival = Fixed("New Year Festival", 12, 28) with { DurationDays = 10 };
        NotableDateService service = BuildService(festival);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(new DateTime(2026, 1, 2));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 2), new DateTime(2026, 1, 2));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a multi-day span anchored in the previous year, whose tail overlaps the
    /// queried window, is emitted on both paths when the window begins in January and the anchor
    /// lies in December of the prior year. After pipeline promotion this scenario succeeds
    /// because <c>GetNotableDates(start, end)</c> now delegates to the same range pipeline that
    /// materialises prior-year anchors whose multi-day span overlaps the window.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenMultiDaySpanCrossesStartBoundaryAcrossYears_ShouldMatch()
    {
        NotableDateRule festival = Fixed("New Year Festival", 12, 28) with { DurationDays = 10 };
        NotableDateService service = BuildService(festival);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a multi-day span anchored inside the window whose tail spills out of the
    /// window is emitted identically on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenMultiDaySpanCrossesEndBoundary_ShouldMatch()
    {
        NotableDateRule festival = Fixed("Easter Carnival", 4, 12) with { DurationDays = 7 };
        NotableDateService service = BuildService(festival);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 15));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 15));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that an algorithmic anchor outside the requested window whose offset rule projects
    /// inside the window is emitted on both paths. Easter Sunday (5 April 2026, outside window)
    /// drives Start of Lent (Easter − 46 = 18 Feb 2026, inside Q1 window).
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenAlgorithmicAnchorOutsideWindowAndOffsetInside_ShouldMatch()
    {
        NotableDateRule easter = new()
        {
            Name = "Easter Sunday",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmType = typeof(EasterSundayNotableDateAlgorithm),
        };

        NotableDateRule lent = new()
        {
            Name = "Start of Lent",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            AnchorRuleName = "Easter Sunday",
            OffsetDays = -46,
        };

        NotableDateService service = BuildService(easter, lent);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that an offset rule whose anchor lies outside the queried window but whose
    /// projected occurrence lies inside is emitted identically on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenOffsetOccurrenceInsideWindowAnchorOutside_ShouldMatch()
    {
        NotableDateRule christmas = Fixed("Christmas Day", 12, 25, nonWorking: true);
        NotableDateRule boxing = new()
        {
            Name = "Boxing Day",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Christmas Day",
            OffsetDays = 1,
        };

        NotableDateService service = BuildService(christmas, boxing);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(
            new DateTime(2026, 12, 26), new DateTime(2026, 12, 31));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 26), new DateTime(2026, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that the filtered range overload produces output equivalent to the pipeline's
    /// <c>filter</c> argument.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenFilteredFullYearWindow_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1, NotableDateCategory.Holiday, nonWorking: true),
            Fixed("Mothers Day", 5, 12, NotableDateCategory.Cultural),
            Fixed("Christmas Day", 12, 25, NotableDateCategory.Holiday, nonWorking: true));

        NotableDateFilter holidayFilter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026, holidayFilter);
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), filter: holidayFilter);

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that a parent-territory query that matches a child-scoped rule yields identical
    /// results on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenTerritoryParentMatchesChildRule_ShouldMatch()
    {
        NotableDateService service = BuildService(
            Fixed("Picnic Day", 8, 4, territory: "AU-NT"),
            Fixed("Labour Day", 5, 1, territory: "AU"));

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026, territoryCode: "AU-NT");
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), territoryCode: "AU-NT");

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that calendar-type filtering excludes the same set of rules on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenCalendarTypeFilterExcludesRule_ShouldMatch()
    {
        NotableDateRule gregorian = Fixed("Christmas Day", 12, 25) with { CalendarType = typeof(GregorianCalendar) };
        NotableDateService service = BuildService(gregorian);

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026, calendarType: typeof(JulianCalendar));
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
            calendarType: typeof(JulianCalendar));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that an unscoped override removal suppresses the same rule on both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenOverrideRemovalSuppressesRule_ShouldMatch()
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1);
        TestOverrideProvider provider = new(
            removals: new[] { new RuleRemoval("Holiday") },
            additions: Array.Empty<NotableDateRule>());

        NotableDateService service = new(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = new[] { (INotableDateRuleOverrideProvider)provider },
            });

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026);
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Verifies that an override addition replacing a base rule of the same name surfaces on
    /// both paths.
    /// </summary>
    [TestMethod]
    public void Equivalence_WhenOverrideAdditionReplacesBaseRule_ShouldMatch()
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1);
        NotableDateRule replacement = Fixed("Holiday", 7, 4);
        TestOverrideProvider provider = new(
            removals: new[] { new RuleRemoval("Holiday") },
            additions: new[] { replacement });

        NotableDateService service = new(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = new[] { (INotableDateRuleOverrideProvider)provider },
            });

        IReadOnlyList<NotableDate> legacy = service.GetNotableDates(2026);
        IReadOnlyList<NotableDate> pipeline = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        AssertEquivalent(legacy, pipeline);
    }

    /// <summary>
    /// Compact comparison key extracted from each emitted <see cref="NotableDate" />. Equality is
    /// based on the observed date, end date, name, territory, and adjustment flag — the surface
    /// area both engines are contractually responsible for producing.
    /// </summary>
    private readonly record struct EquivalenceKey(
        DateTime Date,
        DateTime EndDate,
        string Name,
        string? TerritoryCode,
        bool WasAdjusted)
    {
        public override string ToString() =>
            $"{Date:yyyy-MM-dd}..{EndDate:yyyy-MM-dd} {Name}{(TerritoryCode is null ? string.Empty : $" [{TerritoryCode}]")}{(WasAdjusted ? "*" : string.Empty)}";
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Filter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Houses the bespoke filter-integration tests for <see cref="NotableDateService" /> that do not fit the
/// "year + filter → expected name set" KAT consolidation in
/// <see cref="NotableDateServiceTests.FilterScenarios" />. The kept tests assert ordering, the cache
/// invariant, null-filter exception contracts on the three <c>GetNotableDates</c> overloads, the
/// adjustment-flag-and-resolved-date behaviour of <see cref="NotableDateFilter.WasAdjusted" />, and the
/// MultiRuleXml IsNonWorkingDay sweep (whose assertion shape is a non-empty universal predicate over the
/// result, not a name-set equivalence).
/// </summary>
public sealed partial class NotableDateServiceTests
{
    private static NotableDateRule FixedWithTags(string name, int month, int day, NotableDateCategory category, bool nonWorking, ImmutableHashSet<string> tags) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = category,
            Month = month,
            Day = day,
            IsNonWorkingDay = nonWorking,
            Tags = tags,
        };

    private static NotableDateService BuildServiceFromXml(string xml)
    {
        NotableDateRule[] rules = [.. NotableDateRuleParser.ParseXml(xml)];
        return BuildService(rules);
    }

    // --------------------------------------------------------------------------------------
    // Ordering and cache invariants — non-name-set assertions, kept bespoke.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
    /// returns results ordered by anchor date. The ordering contract cannot be encoded as set equivalence.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilter_ShouldReturnResultsOrderedByDate()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday),
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Holiday B", 7, 4, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0].Date <= results[1].Date && results[1].Date <= results[2].Date);
    }

    /// <summary>
    /// Verifies that a filtered query does not affect subsequent unfiltered queries — a stateful
    /// two-call contract that cannot be expressed as a single per-row KAT.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilter_ShouldNotPolluteCacheForUnfilteredQuery()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 6, 1, NotableDateCategory.Observance));

        IReadOnlyList<NotableDate> filtered = service.GetNotableDates(2024, NotableDateFilter.ForCategory(NotableDateCategory.Holiday));
        IReadOnlyList<NotableDate> unfiltered = service.GetNotableDates(2024);

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual(2, unfiltered.Count);
    }

    // --------------------------------------------------------------------------------------
    // Null-filter exception contracts on each of the three GetNotableDates overloads.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(2024, (NotableDateFilter)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithDateRangeAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), (NotableDateFilter)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithSingleDateAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2024, 1, 1), (NotableDateFilter)null!);
        });
    }

    // --------------------------------------------------------------------------------------
    // WasAdjusted filter — asserts the WasAdjusted flag on the returned occurrence, not just names.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> returns only the adjusted occurrence when
    /// an observance adjustment fires. Asserts the <c>WasAdjusted</c> flag and the resolved date on the
    /// returned <see cref="NotableDate" />, so the test stays bespoke.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWasAdjustedFilter_WhenAdjustmentFires_ShouldReturnOnlyAdjustedOccurrence()
    {
        // 1 January 2022 is a Saturday; the IfWeekend trigger fires and moves it to Monday 3 January.
        NotableDateRule rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-roll",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }),
        };

        NotableDateService service = BuildService(rule);
        var filter = NotableDateFilter.WasAdjusted();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2022, filter);

        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2022, 1, 3), results[0].Date);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> returns an empty list when no adjustment
    /// fires for the queried year. Companion to the firing-case test above; kept alongside for proximity.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWasAdjustedFilter_WhenNoAdjustmentFires_ShouldReturnEmptyList()
    {
        // 1 January 2024 is a Monday; the IfWeekend trigger does not fire.
        NotableDateRule rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-roll",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }),
        };

        NotableDateService service = BuildService(rule);
        var filter = NotableDateFilter.WasAdjusted();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(0, results.Count);
    }

    // --------------------------------------------------------------------------------------
    // MultiRuleXml IsNonWorkingDay — non-empty universal predicate, not a name-set equivalence.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> applied to a service built from
    /// <see cref="NotableDateRuleParserTests.MultiRuleXml" /> returns only dates flagged non-working,
    /// confirming that the XML <c>nonWorking</c> attribute flows through to the filter pipeline. The
    /// assertion shape (non-empty AND every entry passes a predicate) does not fit the per-row name-set KAT.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedMultiRuleXml_WithNonWorkingFilter_ShouldReturnOnlyNonWorkingDates()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.MultiRuleXml);
        var filter = NotableDateFilter.IsNonWorkingDay();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.IsTrue(results.Count > 0, "Expected at least one non-working date from MultiRuleXml.");
        Assert.IsTrue(results.All(d => d.IsNonWorkingDay), "Every returned date must be flagged as a non-working day.");
    }
}

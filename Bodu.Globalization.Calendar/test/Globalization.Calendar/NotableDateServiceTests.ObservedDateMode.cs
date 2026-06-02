// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.ObservedDateMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies observed-date emission semantics: window-independent results, observed-date modes, and deterministic
/// adjustment priority.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that the set of notable dates reported for a given day does not change with the width of the query
    /// window — querying the base day alone and querying a wider range that also covers the observed substitute must
    /// agree about what falls on the base day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAdjustmentFires_DayResultShouldBeIndependentOfWindowWidth()
    {
        // 1 January 2022 is a Saturday; the weekend roll moves the observed occurrence to Monday 3 January.
        NotableDateService service = BuildService(
            NewYearRollingToMonday());

        var baseDay = new DateTime(2022, 1, 1);

        IReadOnlyList<NotableDate> narrow = service.GetNotableDates(baseDay);
        IReadOnlyList<NotableDate> wide = service.GetNotableDates(baseDay, new DateTime(2022, 1, 3));

        var onBaseDayNarrow = narrow.Where(d => d.Date == baseDay).Select(d => d.Name).OrderBy(n => n).ToList();
        var onBaseDayWide = wide.Where(d => d.Date == baseDay).Select(d => d.Name).OrderBy(n => n).ToList();

        CollectionAssert.AreEqual(onBaseDayNarrow, onBaseDayWide);
    }

    /// <summary>
    /// Verifies that when several adjustments activate, the highest-precedence (lowest <see cref="ObservanceAdjustment.Priority" />)
    /// adjustment wins deterministically, independent of authored order.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMultipleAdjustmentsActivate_ShouldApplyHighestPrecedenceOnly()
    {
        // Base is Tuesday 6 January 2026; both adjustments always activate but shift by different offsets.
        var rule = new NotableDateRule
        {
            Name = "Priority Test",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 6,
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "low-priority", Trigger = AdjustmentTrigger.Always, Action = AdjustmentAction.AddDays, OffsetDays = 5, Priority = 20 },
                new ObservanceAdjustment { Key = "high-priority", Trigger = AdjustmentTrigger.Always, Action = AdjustmentAction.AddDays, OffsetDays = 2, Priority = 10 },
            ],
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        var priorityTest = results.Where(d => d.Name == "Priority Test").ToList();

        Assert.AreEqual(1, priorityTest.Count);
        Assert.AreEqual(new DateTime(2026, 1, 8), priorityTest[0].Date, "The priority-10 adjustment (+2 days) should win over the priority-20 adjustment (+5 days).");
    }

    /// <summary>
    /// Verifies that under <see cref="ObservedDateMode.ActualOnly" /> the actual date is emitted and the observed
    /// substitute is suppressed.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenActualOnly_ShouldEmitActualNotObserved()
    {
        NotableDateService service = BuildService(ObservedDateMode.ActualOnly, NewYearRollingToMonday());

        var dates = service.GetNotableDates(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3))
            .Where(d => d.Name == "New Year's Day")
            .ToList();

        Assert.AreEqual(1, dates.Count);
        Assert.AreEqual(new DateTime(2022, 1, 1), dates[0].Date);
        Assert.IsFalse(dates[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that under <see cref="ObservedDateMode.ObservedOnly" /> the observed substitute supersedes the actual date.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenObservedOnly_ShouldEmitObservedNotActual()
    {
        NotableDateService service = BuildService(ObservedDateMode.ObservedOnly, NewYearRollingToMonday());

        var dates = service.GetNotableDates(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3))
            .Where(d => d.Name == "New Year's Day")
            .ToList();

        Assert.AreEqual(1, dates.Count);
        Assert.AreEqual(new DateTime(2022, 1, 3), dates[0].Date);
        Assert.IsTrue(dates[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that under <see cref="ObservedDateMode.ActualAndObserved" /> both the actual date and the observed
    /// substitute are emitted as separate occurrences.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenActualAndObserved_ShouldEmitBoth()
    {
        NotableDateService service = BuildService(ObservedDateMode.ActualAndObserved, NewYearRollingToMonday());

        var dates = service.GetNotableDates(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3))
            .Where(d => d.Name == "New Year's Day")
            .OrderBy(d => d.Date)
            .ToList();

        Assert.AreEqual(2, dates.Count);
        Assert.AreEqual(new DateTime(2022, 1, 1), dates[0].Date);
        Assert.IsFalse(dates[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2022, 1, 3), dates[1].Date);
        Assert.IsTrue(dates[1].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a per-query observed-date mode supplied to
    /// <see cref="NotableDateService.ResolveNotableDatesInRange" /> overrides the service-wide default.
    /// </summary>
    [TestMethod]
    public void ResolveNotableDatesInRange_WhenPerQueryModeSupplied_ShouldOverrideServiceDefault()
    {
        NotableDateService service = BuildService(ObservedDateMode.ObservedOnly, NewYearRollingToMonday());

        var dates = service.ResolveNotableDatesInRange(
                new DateTime(2022, 1, 1),
                new DateTime(2022, 1, 3),
                observedDates: ObservedDateMode.ActualAndObserved)
            .Where(d => d.Name == "New Year's Day")
            .ToList();

        Assert.AreEqual(2, dates.Count, "The per-query ActualAndObserved mode should override the service-wide ObservedOnly default.");
    }

    /// <summary>
    /// Builds a service configured with the supplied observed-date mode over the given rules.
    /// </summary>
    /// <param name="mode">The observed-date mode to configure as the service default.</param>
    /// <param name="rules">The rules to expose.</param>
    /// <returns>The constructed service.</returns>
    private static NotableDateService BuildService(ObservedDateMode mode, params NotableDateRule[] rules) =>
        new(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(rules)],
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { ObservedDates = mode });

    /// <summary>
    /// Creates a fixed New Year's Day rule that rolls a weekend occurrence forward by two days (Saturday to Monday).
    /// </summary>
    /// <returns>The constructed rule.</returns>
    private static NotableDateRule NewYearRollingToMonday() =>
        new()
        {
            Name = "New Year's Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            IsNonWorkingDay = true,
            Adjustments =
            [
                new ObservanceAdjustment { Key = "weekend-roll", Trigger = AdjustmentTrigger.IfWeekend, Action = AdjustmentAction.AddDays, OffsetDays = 2, Priority = 10 },
            ],
        };
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.MultiOverride.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateService" /> applies multiple override providers in registration order,
/// honours filters across the year-based queries, and exposes year-cache invalidation per-year and globally.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that when two override providers add a rule sharing the same composite (Name, Territory) key, the
    /// later provider in registration order wins.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMultipleOverrideProvidersAddSameRule_ShouldUseLaterProvider()
    {
        NotableDateRule earlyRule = Fixed("Substitute Holiday", 6, 1);
        NotableDateRule lateRule = Fixed("Substitute Holiday", 7, 1);

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider()],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders =
                [
                    new TestOverrideProvider(removals: [], additions: [earlyRule]),
                    new TestOverrideProvider(removals: [], additions: [lateRule]),
                ],
            });

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);

        IReadOnlyList<NotableDate> matches = emitted.Where(n => n.Name == "Substitute Holiday").ToList();
        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual(new DateTime(2026, 7, 1), matches[0].Date);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> applies
    /// the rule-level filter so non-matching rules are not materialised.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFilterAppliedToFullYear_ShouldExcludeNonMatchingRules()
    {
        NotableDateService service = BuildService(
            Fixed("Public", 1, 26, NotableDateCategory.Holiday),
            Fixed("Religious", 3, 14, NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(
            2026,
            NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, emitted.Count);
        Assert.AreEqual("Religious", emitted[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// applies the rule-level filter so non-matching rules are not materialised.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFilterAppliedToDateRange_ShouldExcludeNonMatchingRules()
    {
        NotableDateService service = BuildService(
            Fixed("Public", 1, 26, NotableDateCategory.Holiday),
            Fixed("Religious", 3, 14, NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, emitted.Count);
        Assert.AreEqual("Religious", emitted[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> applies
    /// the rule-level filter to a single-day query.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFilterAppliedToSingleDay_ShouldExcludeNonMatchingRules()
    {
        NotableDateService service = BuildService(
            Fixed("Public", 1, 26, NotableDateCategory.Holiday),
            Fixed("Religious", 3, 14, NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> emittedReligious = service.GetNotableDates(
            new DateTime(2026, 3, 14),
            NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> emittedFiltered = service.GetNotableDates(
            new DateTime(2026, 1, 26),
            NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, emittedReligious.Count);
        Assert.AreEqual("Religious", emittedReligious[0].Name);
        Assert.AreEqual(0, emittedFiltered.Count);
    }

    /// <summary>
    /// Verifies that an adjustment using <see cref="AdjustmentAction.MoveToNextWorkingDay" /> drives the re-entry
    /// guard in <c>GetOrGenerateYear</c> without infinite recursion or premature year-boundary expansion.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAdjustmentChainsAcrossYearBoundary_ShouldEmitFinalSubstitute()
    {
        NotableDateRule yearEnd = new()
        {
            Name = "Year-End Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 31,
            IsNonWorkingDay = true,
            Adjustments = [new ObservanceAdjustment
            {
                Key = "weekend-substitute",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWorkingDay,
                IsNonWorkingDay = true,
            }],
        };

        NotableDateService service = BuildService(yearEnd);

        // 31 Dec 2022 was Saturday → substitute should appear in early January 2023.
        IReadOnlyList<NotableDate> jan2023 = service.GetNotableDates(2023);

        Assert.IsTrue(jan2023.Any(d => d.WasAdjusted && d.Name == "Year-End Holiday"));
    }
}

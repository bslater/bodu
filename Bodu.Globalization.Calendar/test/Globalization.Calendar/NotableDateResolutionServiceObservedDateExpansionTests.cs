// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceObservedDateExpansionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests observed-date projection on <see cref="NotableDateService" /> range queries for adjusted occurrences whose
/// source anchor falls outside the requested window.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceObservedDateExpansionTests
{
    /// <summary>
    /// Verifies that an observed-date query can return an adjusted occurrence anchored in the previous year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedDateIsProducedByPreviousYearAnchor_ShouldReturnAdjustedOccurrence()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2023, 1, 3),
            new DateTime(2023, 1, 3));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Year-End Holiday", actual[0].Name);
        Assert.AreEqual(new DateTime(2023, 1, 3), actual[0].Date);
        Assert.IsTrue(actual[0].WasAdjusted);
        Assert.IsNotNull(actual[0].AdjustmentReason);
        Assert.AreEqual(new DateTime(2022, 12, 31), actual[0].AdjustmentReason!.OriginalDate);
    }

    /// <summary>
    /// Verifies that neighbouring blockers materialised to calculate the observed date are not emitted to the caller.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedDateQueryMaterialisesNextYearBlocker_ShouldNotEmitBlocker()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2023, 1, 3),
            new DateTime(2023, 1, 3));

        Assert.IsFalse(actual.Any(date =>
            date.Name == "New Year Second Day" &&
            date.Date == new DateTime(2023, 1, 2)));
    }

    /// <summary>
    /// Verifies that an adjusted occurrence sourced from the padded materialisation window is not emitted when it does not
    /// intersect the requested observed-date window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedDateDoesNotIntersectAdjustedDate_ShouldNotReturnOccurrence()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2023, 1, 4),
            new DateTime(2023, 1, 4));

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that date-range observed projection includes the prior-year adjusted occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedRangeContainsAdjustedDateFromPreviousYearAnchor_ShouldReturnAdjustedOccurrence()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2023, 1, 1),
            new DateTime(2023, 1, 7));

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2023, 1, 3) &&
            date.WasAdjusted));
    }

    private static NotableDateService CreateService(params NotableDateRule[] rules) =>
        new(
            ruleProviders: [new InMemoryRuleProvider(rules)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    private static NotableDateRule FixedPublicHolidayRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(
                new ObservanceAdjustment
                {
                    Key = "weekend-substitute",
                    Trigger = AdjustmentTrigger.IfWeekend,
                    Action = AdjustmentAction.MoveToNextNonWorkingDay,
                    IsNonWorkingDay = true,
                }),
        };

    private static NotableDateRule FixedNonWorkingRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray<ObservanceAdjustment>.Empty,
        };

    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        public InMemoryRuleProvider(IReadOnlyList<NotableDateRule> rules)
        {
            this._rules = rules;
        }

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}

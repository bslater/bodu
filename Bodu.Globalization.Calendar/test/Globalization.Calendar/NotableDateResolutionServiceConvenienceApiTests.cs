// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceConvenienceApiTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the convenience overloads on <see cref="NotableDateService" />: the year overload, single-day overload,
/// date-range overload, and <see cref="NotableDateService.IsNonWorkingDay" />.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceConvenienceApiTests
{
    /// <summary>
    /// Verifies that the year overload returns occurrences whose observed date falls within the supplied civil year,
    /// matching the equivalent <c>(Jan 1, Dec 31)</c> range query under the same pipeline.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenYearRequested_ShouldReturnDatesObservedInYear()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(2022);

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2022, 12, 31) &&
            !date.WasAdjusted));

        // The adjusted observation falls on 2023-01-03 — outside 2022, so it is not emitted by the year overload.
        Assert.IsFalse(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2023, 1, 3)));

        // New Year Second Day is anchored in 2023, so the 2022 year overload does not emit it.
        Assert.IsFalse(actual.Any(date =>
            date.Name == "New Year Second Day" &&
            date.Date == new DateTime(2023, 1, 2)));
    }

    /// <summary>
    /// Verifies that a single-date query returns an adjusted observation produced by a previous-year anchor.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenObservedDateRequested_ShouldReturnAdjustedOccurrenceFromPreviousYearAnchor()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(new DateTime(2023, 1, 3));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Year-End Holiday", actual[0].Name);
        Assert.AreEqual(new DateTime(2023, 1, 3), actual[0].Date);
        Assert.IsTrue(actual[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a date-range query returns multi-day spans whose observed dates intersect the requested window.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenObservedRangeRequested_ShouldReturnDatesIntersectingRange()
    {
        NotableDateService service = CreateService(
            FixedRule("Religious Festival", month: 6, day: 10) with
            {
                DurationDays = 5,
            },
            FixedRule("Later Date", month: 6, day: 20));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 14),
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Religious Festival", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 6, 10), actual[0].Date);
        Assert.AreEqual(new DateTime(2024, 6, 14), actual[0].EndDate);
    }

    /// <summary>
    /// Verifies that a single-date query honours the supplied territory context, returning subdivision-scoped rules
    /// when the requested territory is the parent.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTerritoryMatchesSubdivision_ShouldReturnScopedOccurrence()
    {
        NotableDateService service = CreateService(
            FixedRule("NSW Observance", month: 8, day: 5) with
            {
                TerritoryCode = "AU-NSW",
            });

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 8, 5),
            territoryCode: "AU");

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("NSW Observance", actual[0].Name);
        Assert.AreEqual("AU-NSW", actual[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that weekend dates are treated as non-working days.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsWeekend_ShouldReturnTrue()
    {
        NotableDateService service = CreateService();

        var actual = service.IsNonWorkingDay(new DateTime(2024, 1, 6));

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that observed substitute dates are treated as non-working days.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsObservedSubstituteHoliday_ShouldReturnTrue()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        var actual = service.IsNonWorkingDay(new DateTime(2023, 1, 3));

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that ordinary weekdays with no non-working notable date are not treated as non-working.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsOrdinaryWeekday_ShouldReturnFalse()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        var actual = service.IsNonWorkingDay(new DateTime(2023, 1, 4));

        Assert.IsFalse(actual);
    }

    private static NotableDateService CreateService(params NotableDateRule[] rules) =>
        new(
            ruleProviders: [new InMemoryRuleProvider(rules)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    private static NotableDateRule FixedRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Religious,
            Month = month,
            Day = day,
            IsNonWorkingDay = false,
            Adjustments = ImmutableArray<ObservanceAdjustment>.Empty,
        };

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

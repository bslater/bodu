// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceConvenienceApiTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that the year overload returns occurrences by their observed date under the default
    /// <see cref="ObservedDateMode.ObservedOnly" /> mode: a holiday whose weekend substitute rolls into the next civil
    /// year is excluded from the current year and reported in the following year, keeping the result window-independent.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenYearRequested_ShouldReturnDatesObservedInYear()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> year2022 = service.GetNotableDates(2022);

        // 31 Dec 2022 is a Saturday; under ObservedOnly the substitute moves the observed occurrence to 3 Jan 2023, so
        // the holiday is not part of 2022's observed dates (the actual 31 Dec date is superseded).
        Assert.IsFalse(year2022.Any(date => date.Name == "Year-End Holiday"),
            "Under ObservedOnly the actual 31 Dec is superseded and the observed substitute falls in 2023.");

        // The 2 Jan 2023 occurrence of New Year Second Day lies outside 2022, so the year overload does not emit it.
        Assert.IsFalse(year2022.Any(date => date.Name == "New Year Second Day" && date.Date == new DateTime(2023, 1, 2)));

        // The observed substitute is reported in 2023, demonstrating the occurrence moved across the year boundary.
        IReadOnlyList<NotableDate> year2023 = service.GetNotableDates(2023);
        Assert.IsTrue(year2023.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2023, 1, 3) &&
            date.WasAdjusted));
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
            Adjustments = [],
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
            Adjustments = [new ObservanceAdjustment
                {
                    Key = "weekend-substitute",
                    Trigger = AdjustmentTrigger.IfWeekend,
                    Action = AdjustmentAction.MoveToNextWorkingDay,
                    IsNonWorkingDay = true,
                }],
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
            Adjustments = [],
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

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceConvenienceApiTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the API-compatible convenience members on <see cref="NotableDateResolutionService" />.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceConvenienceApiTests
{
    /// <summary>
    /// Verifies that a year query uses anchor-date projection and returns adjusted dates produced by anchors in that year.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenYearRequested_ShouldReturnDatesAnchoredInYear()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(2022);

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2022, 12, 31) &&
            !date.WasAdjusted));

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.Date == new DateTime(2023, 1, 3) &&
            date.WasAdjusted));

        Assert.IsFalse(actual.Any(date =>
            date.Name == "New Year Second Day" &&
            date.Date == new DateTime(2023, 1, 2)));
    }

    /// <summary>
    /// Verifies that a single-date query uses observed-date projection and can return an adjusted date from a previous-year
    /// anchor.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenObservedDateRequested_ShouldReturnAdjustedOccurrenceFromPreviousYearAnchor()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(new DateTime(2023, 1, 3));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Year-End Holiday", actual[0].Name);
        Assert.AreEqual(new DateTime(2023, 1, 3), actual[0].Date);
        Assert.IsTrue(actual[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a date-range query uses observed-date projection.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenObservedRangeRequested_ShouldReturnDatesIntersectingRange()
    {
        NotableDateResolutionService service = CreateService(
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
    /// Verifies that single-date query honours the supplied territory context.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTerritoryMatchesSubdivision_ShouldReturnScopedOccurrence()
    {
        NotableDateResolutionService service = CreateService(
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
        NotableDateResolutionService service = CreateService();

        bool actual = service.IsNonWorkingDay(new DateTime(2024, 1, 6));

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that observed substitute dates are treated as non-working days.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsObservedSubstituteHoliday_ShouldReturnTrue()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        bool actual = service.IsNonWorkingDay(new DateTime(2023, 1, 3));

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that ordinary weekdays with no non-working notable date are not treated as non-working.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateIsOrdinaryWeekday_ShouldReturnFalse()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        bool actual = service.IsNonWorkingDay(new DateTime(2023, 1, 4));

        Assert.IsFalse(actual);
    }

    private static NotableDateResolutionService CreateService(params NotableDateRule[] rules) =>
        new(ruleProviders: new[] { new InMemoryRuleProvider(rules) });

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

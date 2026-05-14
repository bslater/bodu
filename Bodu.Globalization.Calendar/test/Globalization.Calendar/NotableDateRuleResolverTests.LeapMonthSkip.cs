// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.LeapMonthSkip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the lunisolar leap-month skip path in <see cref="NotableDateRuleResolver" /> for Fixed rules authored
/// against <see cref="SysGlobal.ChineseLunisolarCalendar" /> with <see cref="NotableDateRule.SkipLeapMonth" /> set.
/// </summary>
[TestClass]
public sealed class NotableDateRuleResolverLeapMonthSkipTests
{
    /// <summary>
    /// Verifies that the conventional fifth lunar month resolves to the correct Gregorian date in 2024 (a non-leap
    /// lunisolar year) — Dragon Boat Festival, 5th day of the 5th lunar month, fell on 10 June 2024.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenLunisolarRuleInNonLeapYear_ShouldResolveExpectedDate()
    {
        NotableDateRule rule = new()
        {
            Name = "Dragon Boat Festival",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 5,
            Day = 5,
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
            SkipLeapMonth = true,
        };

        NotableDateRuleResolver resolver = new(new[] { rule });

        DateTime? date = resolver.ResolveAnchorDate(rule, 2024);

        Assert.AreEqual(new DateTime(2024, 6, 10), date);
    }

    /// <summary>
    /// Verifies that the conventional fifth lunar month with <see cref="NotableDateRule.SkipLeapMonth" /> set resolves
    /// past the intercalary month in 2023 (a leap lunisolar year with leap month 2) — Dragon Boat 2023 fell on
    /// 22 June 2023.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenLunisolarRuleInLeapYearAndMonthAfterIntercalary_ShouldSkipLeapMonth()
    {
        NotableDateRule rule = new()
        {
            Name = "Dragon Boat Festival",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 5,
            Day = 5,
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
            SkipLeapMonth = true,
        };

        NotableDateRuleResolver resolver = new(new[] { rule });

        DateTime? date = resolver.ResolveAnchorDate(rule, 2023);

        Assert.AreEqual(new DateTime(2023, 6, 22), date);
    }

    /// <summary>
    /// Verifies that a Fixed lunisolar rule whose calendar month index would exceed the months in the year returns
    /// <see langword="null" /> — for example, requesting month 13 in a non-leap year.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenLunisolarRuleMonthExceedsCalendarMonths_ShouldReturnNull()
    {
        NotableDateRule rule = new()
        {
            Name = "Beyond Last Month",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 13,
            Day = 1,
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
            SkipLeapMonth = true,
        };

        NotableDateRuleResolver resolver = new(new[] { rule });

        DateTime? date = resolver.ResolveAnchorDate(rule, 2024);

        Assert.IsNull(date);
    }

    /// <summary>
    /// Verifies that requesting a Fixed lunisolar day larger than the days-in-the-resolved-month returns
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenLunisolarRuleDayExceedsMonthLength_ShouldReturnNull()
    {
        NotableDateRule rule = new()
        {
            Name = "Beyond Month End",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 5,
            Day = 31,
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
            SkipLeapMonth = true,
        };

        NotableDateRuleResolver resolver = new(new[] { rule });

        DateTime? date = resolver.ResolveAnchorDate(rule, 2024);

        Assert.IsNull(date);
    }

    /// <summary>
    /// Verifies that a Fixed lunisolar rule whose year falls outside the calendar's supported range returns
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenLunisolarRuleYearOutsideSupportedRange_ShouldReturnNull()
    {
        NotableDateRule rule = new()
        {
            Name = "Way Back",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 1,
            Day = 1,
            CalendarType = typeof(SysGlobal.ChineseLunisolarCalendar),
            SkipLeapMonth = true,
        };

        NotableDateRuleResolver resolver = new(new[] { rule });

        // ChineseLunisolarCalendar.MinSupportedDateTime is 1901; year 1800 is outside the range.
        DateTime? date = resolver.ResolveAnchorDate(rule, 1800);

        Assert.IsNull(date);
    }
}

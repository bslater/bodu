// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.WeekOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="CalendarWeekRule" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenCalendarWeekRuleIsInvalid_ShouldThrowExactly()
    {
        var date = new DateOnly(2024, 01, 01);
        var invalidRule = (CalendarWeekRule)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.WeekOfMonth(invalidRule, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that the parameterless overload falls back to <see cref="CultureInfo.CurrentCulture" /> when no culture is supplied.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenCultureIsNull_ShouldUseCurrentCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); // Starts week on Monday
            DateOnly input = new(2024, 01, 08); // 2nd Monday of Jan 2024
            int week = input.WeekOfMonth(); // Should use fr-FR's firstDayOfWeek (Monday)

            Assert.AreEqual(2, week); // Jan 8 falls in 2nd full week
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var date = new DateOnly(2024, 01, 01);
        var invalidDay = (DayOfWeek)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.WeekOfMonth(CalendarWeekRule.FirstDay, invalidDay);
        });
    }

    /// <summary>
    /// Verifies that the culture overload yields the same week index as an explicit <c>(rule, firstDay)</c> call across a representative set of cultures.
    /// </summary>
    [TestMethod]
    [DataRow("en-US")]
    [DataRow("en-GB")]
    [DataRow("de-DE")]
    [DataRow("fr-FR")]
    [DataRow("ar-SA")]
    [DataRow("he-IL")]
    public void WeekOfMonth_WhenGivenVariousCultures_ShouldReturnConsistent(string cultureName)
    {
        var date = new DateOnly(2024, 04, 05);
        var culture = new CultureInfo(cultureName);
        int expected = date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        int actual = date.WeekOfMonth(culture);
        Assert.AreEqual(expected, actual, $"Culture: {cultureName}");
    }

    /// <summary>
    /// Verifies that the culture overload reads <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> from the supplied culture.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenUsingCultureInfo_ShouldRespectCultureSettings()
    {
        var date = new DateOnly(2024, 05, 15);
        var culture = new CultureInfo("en-US") { DateTimeFormat = { FirstDayOfWeek = DayOfWeek.Sunday } };
        int expected = date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        int actual = date.WeekOfMonth(culture);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the parameterless overload produces the same result as an explicit call with <see cref="CultureInfo.CurrentCulture" />'s rule and first-day-of-week.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenUsingDefaultCulture_ShouldMatchExplicitCall()
    {
        var date = new DateOnly(2024, 05, 15);
        int expected = date.WeekOfMonth(CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule,
                                        CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
        int actual = date.WeekOfMonth();
        Assert.AreEqual(expected, actual);
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.WeekOfMonth(DateOnly, CalendarWeekRule, DayOfWeek)" /> returns the expected week index for each <c>(date, rule, firstDay)</c> triple.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.WeekOfMonthCalendarWeekRuleTestData), typeof(DateTimeExtensionsTests))]
    public void WeekOfMonth_WithCalendarWeekAndRule_ShouldReturnExpected(DateTime inputDateTime, CalendarWeekRule rule, DayOfWeek firstDay, int expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);

        int actual = input.WeekOfMonth(rule, firstDay);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.WeekOfMonth(DateOnly, CultureInfo)" /> returns the expected week index for each <c>(date, culture)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.WeekOfMonthCultureTestData), typeof(DateTimeExtensionsTests))]
    public void WeekOfMonth_WithCulture_ShouldReturnExpected(DateTime inputDateTime, CultureInfo culture, int expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);

        int actual = input.WeekOfMonth(culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekRule.FirstDay" /> and <see cref="CalendarWeekRule.FirstFullWeek" /> produce
    /// different week numbers for a month whose first day falls mid-week: March 2024 starts on Friday, so under
    /// <see cref="CalendarWeekRule.FirstFullWeek" /> with a Sunday week start, week 1 begins on Sunday 3 March and
    /// every date from there on numbers one lower than under <see cref="CalendarWeekRule.FirstDay" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenFirstOfMonthIsMidWeek_ShouldDistinguishFirstDayFromFirstFullWeek()
    {
        var date = new DateOnly(2024, 3, 8); // Friday in the week beginning Sunday 3 March

        Assert.AreEqual(2, date.WeekOfMonth(CalendarWeekRule.FirstDay, DayOfWeek.Sunday));
        Assert.AreEqual(1, date.WeekOfMonth(CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that under <see cref="CalendarWeekRule.FirstFullWeek" /> a date preceding week 1 of its month returns
    /// the trailing week number of the previous month: 1 March 2024 (Friday) with a Sunday week start belongs to the
    /// week beginning Sunday 25 February, which is week 4 of February.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenDatePrecedesWeekOne_ForFirstFullWeek_ShouldReturnPreviousMonthsTrailingWeek()
    {
        var date = new DateOnly(2024, 3, 1);

        int actual = date.WeekOfMonth(CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);

        Assert.AreEqual(4, actual);
        Assert.AreEqual(new DateOnly(2024, 2, 25).WeekOfMonth(CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday), actual);
    }

}

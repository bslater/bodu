// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.WeekOfMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="CalendarWeekRule" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenCalendarWeekRuleIsInvalid_ShouldThrowExactly()
    {
        var date = new DateTime(2024, 01, 01);
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
            DateTime input = new(2024, 01, 08); // 2nd Monday of Jan 2024
            var week = input.WeekOfMonth(); // Should use fr-FR's firstDayOfWeek (Monday)

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
        var date = new DateTime(2024, 01, 01);
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
        var date = new DateTime(2024, 04, 05);
        var culture = new CultureInfo(cultureName);
        var expected = date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        var actual = date.WeekOfMonth(culture);
        Assert.AreEqual(expected, actual, $"Culture: {cultureName}");
    }

    /// <summary>
    /// Verifies that the culture overload reads <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> from the supplied culture.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenUsingCultureInfo_ShouldRespectCultureSettings()
    {
        var date = new DateTime(2024, 05, 15);
        var culture = new CultureInfo("en-US") { DateTimeFormat = { FirstDayOfWeek = DayOfWeek.Sunday } };
        var expected = date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        var actual = date.WeekOfMonth(culture);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the parameterless overload produces the same result as an explicit call with <see cref="CultureInfo.CurrentCulture" />'s rule and first-day-of-week.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_WhenUsingDefaultCulture_ShouldMatchExplicitCall()
    {
        var date = new DateTime(2024, 05, 15);
        var expected = date.WeekOfMonth(CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule,
                                        CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
        var actual = date.WeekOfMonth();
        Assert.AreEqual(expected, actual);
    }
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.WeekOfMonth(DateTime, CalendarWeekRule, DayOfWeek)" /> returns the expected week index for each <c>(date, rule, firstDay)</c> triple.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekOfMonthCalendarWeekRuleTestData))]
    public void WeekOfMonth_WithCalendarWeekAndRule_ShouldReturnExpected(DateTime input, CalendarWeekRule rule, DayOfWeek firstDay, int expected)
    {
        var actual = input.WeekOfMonth(rule, firstDay);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.WeekOfMonth(DateTime, CultureInfo)" /> returns the expected week index for each <c>(date, culture)</c> pair.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekOfMonthCultureTestData))]
    public void WeekOfMonth_WithCulture_ShouldReturnExpected(DateTime input, CultureInfo culture, int expected)
    {
        var actual = input.WeekOfMonth(culture);
        Assert.AreEqual(expected, actual);
    }

}

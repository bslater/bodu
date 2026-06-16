// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.SimpleMethods.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Age(DateOnly)" /> returns a non-negative integer for any reference date.
    /// </summary>
    [TestMethod]
    public void Age_NoAsAtDate_WhenInvoked_ShouldReturnNonNegativeYears()
    {
        var birth = new DateOnly(2000, 1, 1);
        var age = birth.Age();
        Assert.IsGreaterThanOrEqualTo(0, age);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Age(DateOnly, DateOnly)" /> handles the February-29 birthday on a non-leap year by
    /// shifting to February 28 for the comparison.
    /// </summary>
    [TestMethod]
    public void Age_WhenBirthIsFeb29AndAsAtIsNonLeapYear_ShouldShiftToFeb28()
    {
        var birth = new DateOnly(2000, 2, 29);
        var asAt = new DateOnly(2023, 2, 28);
        Assert.AreEqual(23, birth.Age(asAt));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Age(DateOnly, DateOnly)" /> returns the full number of completed years between
    /// two dates.
    /// </summary>
    [TestMethod]
    [DataRow(2000, 1, 1, 2024, 1, 1, 24, DisplayName = "Same month/day - exact years")]
    [DataRow(2000, 6, 15, 2024, 6, 14, 23, DisplayName = "Day before birthday - prior year")]
    [DataRow(2000, 6, 15, 2024, 6, 16, 24, DisplayName = "Day after birthday - current year")]
    [DataRow(2000, 12, 31, 2024, 4, 18, 23, DisplayName = "AsAt earlier month than birthday - prior year")]
    [DataRow(2000, 1, 1, 1999, 12, 31, 0, DisplayName = "AsAt before birth - clamped to zero")]
    public void Age_WhenInvokedWithAsAtDate_ShouldReturnCompletedYears(int by, int bm, int bd, int ay, int am, int ad, int expected)
    {
        var birth = new DateOnly(by, bm, bd);
        var asAt = new DateOnly(ay, am, ad);
        Assert.AreEqual(expected, birth.Age(asAt));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DayName(DateOnly)" /> returns the localised day name for the date's
    /// <see cref="DayOfWeek" /> using the current culture.
    /// </summary>
    [TestMethod]
    public void DayName_NoCulture_ShouldReturnCurrentCultureDayName()
    {
        var date = new DateOnly(2024, 1, 1); // Monday
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DayOfWeek.Monday), date.DayName());
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DayName(DateOnly, CultureInfo)" /> respects the supplied culture.
    /// </summary>
    [TestMethod]
    public void DayName_WithCulture_ShouldReturnCultureSpecificName()
    {
        var date = new DateOnly(2024, 1, 1); // Monday
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var en = CultureInfo.GetCultureInfo("en-US");
        Assert.AreEqual(fr.DateTimeFormat.GetDayName(DayOfWeek.Monday), date.DayName(fr));
        Assert.AreEqual(en.DateTimeFormat.GetDayName(DayOfWeek.Monday), date.DayName(en));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly)" /> returns the Gregorian day count for the date's month.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 31)]
    [DataRow(2024, 2, 29)]  // leap year
    [DataRow(2023, 2, 28)]  // non-leap year
    [DataRow(2024, 4, 30)]
    public void DaysInMonth_NoArgs_ShouldReturnGregorianDayCount(int year, int month, int expected)
    {
        var date = new DateOnly(year, month, 1);
        Assert.AreEqual(expected, date.DaysInMonth());
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly, Calendar)" /> returns the day count computed using the
    /// supplied <see cref="Calendar" /> instance.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCalendar_ShouldReturnCalendarSpecificCount()
    {
        var date = new DateOnly(2024, 2, 1);
        Assert.AreEqual(29, date.DaysInMonth(new GregorianCalendar()));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly, Calendar)" /> falls back to the current culture's calendar
    /// when the supplied calendar is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCalendar_WhenCalendarIsNull_ShouldFallBackToCurrentCultureCalendar()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var date = new DateOnly(2024, 2, 15);

            Assert.AreEqual(29, date.DaysInMonth((Calendar?)null));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly, CultureInfo)" /> returns the day count computed using the
    /// supplied culture's calendar.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCulture_ShouldReturnCalendarSpecificCount()
    {
        var date = new DateOnly(2024, 2, 1);
        Assert.AreEqual(29, date.DaysInMonth(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly, CultureInfo)" /> falls back to
    /// <see cref="CultureInfo.CurrentCulture" /> when the supplied culture is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCulture_WhenCultureIsNull_ShouldFallBackToCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var date = new DateOnly(2023, 2, 15);

            Assert.AreEqual(28, date.DaysInMonth((CultureInfo?)null));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfMonth" /> identifies month-end dates correctly.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 31, true)]
    [DataRow(2024, 2, 29, true)]
    [DataRow(2023, 2, 28, true)]
    [DataRow(2024, 2, 28, false)]
    [DataRow(2024, 4, 30, true)]
    [DataRow(2024, 4, 15, false)]
    public void IsLastDateOfMonth_ShouldReturnExpectedResult(int year, int month, int day, bool expected)
    {
        var date = new DateOnly(year, month, day);
        Assert.AreEqual(expected, date.IsLastDateOfMonth);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLeapYear(DateOnly)" /> returns the Gregorian leap-year result for the date's year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, true)]
    [DataRow(2023, false)]
    [DataRow(2000, true)]
    [DataRow(1900, false)]
    [DataRow(2100, false)]
    public void IsLeapYear_ShouldReturnGregorianLeapYearResult(int year, bool expected)
    {
        var date = new DateOnly(year, 1, 1);
        Assert.AreEqual(expected, date.IsLeapYear());
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekday(DateOnly)" /> returns <see langword="true" /> for Mon-Fri and
    /// <see langword="false" /> for Sat/Sun under the default weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWeekday_NoArgs_ShouldReturnExpectedResult()
    {
        Assert.IsTrue(new DateOnly(2024, 1, 1).IsWeekday());  // Mon
        Assert.IsTrue(new DateOnly(2024, 1, 5).IsWeekday());  // Fri
        Assert.IsFalse(new DateOnly(2024, 1, 6).IsWeekday()); // Sat
        Assert.IsFalse(new DateOnly(2024, 1, 7).IsWeekday()); // Sun
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekday(DateOnly, WorkingDaysOfWeek, IWeekendDefinitionProvider)" /> respects
    /// a supplied non-default weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WithFridaySaturdayWeekend_ShouldRespectDefinition()
    {
        Assert.IsTrue(new DateOnly(2024, 1, 7).IsWeekday(WorkingDaysOfWeek.SundayToThursday));  // Sun = weekday
        Assert.IsFalse(new DateOnly(2024, 1, 5).IsWeekday(WorkingDaysOfWeek.SundayToThursday)); // Fri = weekend
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend(DateOnly)" /> returns <see langword="true" /> for Sat/Sun and
    /// <see langword="false" /> for Mon-Fri under the default definition.
    /// </summary>
    [TestMethod]
    public void IsWeekend_NoArgs_ShouldReturnExpectedResult()
    {
        Assert.IsFalse(new DateOnly(2024, 1, 1).IsWeekend()); // Mon
        Assert.IsTrue(new DateOnly(2024, 1, 6).IsWeekend());  // Sat
        Assert.IsTrue(new DateOnly(2024, 1, 7).IsWeekend());  // Sun
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend(DateOnly, WorkingDaysOfWeek, IWeekendDefinitionProvider)" />
    /// respects a supplied non-default weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WithFridaySaturdayWeekend_ShouldRespectDefinition()
    {
        Assert.IsTrue(new DateOnly(2024, 1, 5).IsWeekend(WorkingDaysOfWeek.SundayToThursday));  // Fri
        Assert.IsTrue(new DateOnly(2024, 1, 6).IsWeekend(WorkingDaysOfWeek.SundayToThursday));  // Sat
        Assert.IsFalse(new DateOnly(2024, 1, 7).IsWeekend(WorkingDaysOfWeek.SundayToThursday)); // Sun
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek(DateOnly)" /> returns a date at the end of the week containing the
    /// input under the current culture.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_NoCulture_ShouldReturnDateInSameWeek()
    {
        var monday = new DateOnly(2024, 4, 8); // Monday
        DateOnly result = monday.LastDateOfWeek();

        // The result must be at most 6 days after the input and not earlier.
        Assert.IsGreaterThanOrEqualTo(monday, result);
        Assert.IsLessThanOrEqualTo(6, result.DayNumber - monday.DayNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.MonthName(DateOnly)" /> returns the localised month name for the date's month using
    /// the current culture.
    /// </summary>
    [TestMethod]
    public void MonthName_NoCulture_ShouldReturnCurrentCultureMonthName()
    {
        var date = new DateOnly(2024, 4, 15);
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(4), date.MonthName());
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.MonthName(DateOnly, CultureInfo)" /> respects the supplied culture.
    /// </summary>
    [TestMethod]
    public void MonthName_WithCulture_ShouldReturnCultureSpecificName()
    {
        var date = new DateOnly(2024, 4, 15);
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        Assert.AreEqual(fr.DateTimeFormat.GetMonthName(4), date.MonthName(fr));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOccurrence(DateOnly, int, DateOnly)" /> returns the next aligned occurrence,
    /// returns the start when the reference is earlier than the start, and throws for non-positive intervals.
    /// </summary>
    [TestMethod]
    public void NextOccurrence_SmokeTest_ShouldExerciseAllBranches()
    {
        var start = new DateOnly(2024, 1, 1);

        Assert.AreEqual(new DateOnly(2024, 1, 16), start.NextOccurrence(intervalDays: 5, new DateOnly(2024, 1, 12)));
        Assert.AreEqual(start, start.NextOccurrence(intervalDays: 3, new DateOnly(2024, 1, 1).AddDays(-5)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = start.NextOccurrence(intervalDays: 0, new DateOnly(2024, 1, 10));
        });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = start.NextOccurrence(intervalDays: -1, new DateOnly(2024, 1, 10));
        });
    }

}

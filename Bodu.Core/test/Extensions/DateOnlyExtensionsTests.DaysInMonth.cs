// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.DaysInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly)" /> returns the Gregorian day count for the month of the
    /// supplied <see cref="DateOnly" /> value across leap and non-leap years.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.DaysInMonthTestData), typeof(DateTimeExtensionsTests))]
    public void DaysInMonth_WhenCalled_ShouldReturnDaysInMonth(DateTime inputDateTime, int expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);

        int actual = input.DaysInMonth();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a non-Gregorian calendar receives the date projected into its own year/month reckoning: for
    /// 2024-03-15 the <see cref="System.Globalization.UmAlQuraCalendar" /> month is Ramadan 1445 (30 days), whereas
    /// passing the Gregorian components (year 2024) directly would throw <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WhenCalendarIsUmAlQura_ShouldReturnThatCalendarsMonthLength()
    {
        var calendar = new System.Globalization.UmAlQuraCalendar();
        var date = new DateOnly(2024, 3, 15); // 1445-09 (Ramadan) in the Um Al-Qura calendar
        var oracle = date.ToDateTime(TimeOnly.MinValue);

        int actual = date.DaysInMonth(calendar);

        Assert.AreEqual(calendar.GetDaysInMonth(calendar.GetYear(oracle), calendar.GetMonth(oracle)), actual);
        Assert.AreEqual(30, actual);
    }

    /// <summary>
    /// Verifies that the <see cref="System.Globalization.HebrewCalendar" /> returns the length of its own month
    /// containing the date (Adar II 5784 for 2024-03-15, 29 days) rather than interpreting the Gregorian components,
    /// which lie outside the Hebrew calendar's supported year range.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WhenCalendarIsHebrew_ShouldReturnThatCalendarsMonthLength()
    {
        var calendar = new System.Globalization.HebrewCalendar();
        var date = new DateOnly(2024, 3, 15);
        var oracle = date.ToDateTime(TimeOnly.MinValue);

        int actual = date.DaysInMonth(calendar);

        Assert.AreEqual(calendar.GetDaysInMonth(calendar.GetYear(oracle), calendar.GetMonth(oracle)), actual);
        Assert.AreEqual(29, actual);
    }

    /// <summary>
    /// Verifies that a culture whose calendar is non-Gregorian resolves the month length through that calendar's own
    /// year/month reckoning instead of the Gregorian components.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WhenCultureUsesNonGregorianCalendar_ShouldReturnThatCalendarsMonthLength()
    {
        var culture = new System.Globalization.CultureInfo("ar-SA");
        culture.DateTimeFormat.Calendar = new System.Globalization.UmAlQuraCalendar();
        var date = new DateOnly(2024, 3, 15);
        var calendar = culture.DateTimeFormat.Calendar;
        var oracle = date.ToDateTime(TimeOnly.MinValue);

        int actual = date.DaysInMonth(culture);

        Assert.AreEqual(calendar.GetDaysInMonth(calendar.GetYear(oracle), calendar.GetMonth(oracle)), actual);
        Assert.AreEqual(30, actual);
    }

}

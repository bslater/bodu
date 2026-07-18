// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.DaysInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.DaysInYearGregorianCalendarTestData), typeof(DateTimeExtensionsTests))]
    public void DaysInYear_WhenCalled_ShouldReturnCorrectDays(DateTime inputDateTime, int expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        int actual = input.DaysInYear();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInYear" />, when NoCalendarProvided, returns the expected value.
    /// </summary>
    [TestMethod]
    public void DaysInYear_WhenNoCalendarProvided_ShouldUseCurrentCultureCalendar()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            // Set a known culture with a distinct calendar
            var customCulture = new CultureInfo("ar-SA");
            customCulture.DateTimeFormat.Calendar = new UmAlQuraCalendar(); // Has non-Gregorian year lengths
            CultureInfo.CurrentCulture = customCulture;

            DateOnly input = new(2023, 7, 19); // Gregorian date falling in 1445 AH
            int expected = customCulture.DateTimeFormat.Calendar.GetDaysInYear(1445);
            int actual = input.DaysInYear(); // Should use current culture calendar

            Assert.AreEqual(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInYear" />, when UsingCustomCalendar, projects the date into the
    /// calendar's own year reckoning and returns that year's length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.DaysInYearTestData), typeof(DateTimeExtensionsTests))]
    public void DaysInYear_WhenUsingCustomCalendar_ShouldMatchExpected(int year, Calendar calendar, int expectedDays)
    {
        // Build a DateOnly that actually falls in the calendar-native year under test.
        var input = DateOnly.FromDateTime(calendar.ToDateTime(year, 1, 1, 0, 0, 0, 0));

        int actual = input.DaysInYear(calendar);

        Assert.AreEqual(expectedDays, actual, $"{calendar.GetType().Name} returned {actual} days for year {year}.");
    }

    /// <summary>
    /// Verifies that a non-Gregorian calendar receives the date projected into its own year reckoning: for 2024-03-15
    /// the <see cref="UmAlQuraCalendar" /> year is 1445 AH (354 days), whereas passing the Gregorian year 2024 directly
    /// would throw <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DaysInYear_WhenCalendarIsUmAlQura_ShouldReturnThatCalendarsYearLength()
    {
        var calendar = new UmAlQuraCalendar();
        var date = new DateOnly(2024, 3, 15); // 1445 AH in the Um Al-Qura calendar
        var oracle = date.ToDateTime(TimeOnly.MinValue);

        int actual = date.DaysInYear(calendar);

        Assert.AreEqual(calendar.GetDaysInYear(calendar.GetYear(oracle)), actual);
        Assert.AreEqual(354, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInYear" />, when UsingMaxValue, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void DaysInYear_WhenUsingMaxValue_ShouldNotThrow() => Assert.IsGreaterThan(0, DateOnly.MaxValue.DaysInYear());

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInYear" />, when UsingMinValue, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void DaysInYear_WhenUsingMinValue_ShouldNotThrow() => Assert.IsGreaterThan(0, DateOnly.MinValue.DaysInYear());

}

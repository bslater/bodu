// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.FromDayOfYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FromDayOfYear(int, int)" /> maps a one-based ordinal day to the
    /// expected calendar date, accounting for leap years.
    /// </summary>
    [TestMethod]
    [DataRow(2020, 1, 2020, 1, 1)]     // first day
    [DataRow(2020, 60, 2020, 2, 29)]   // day 60 in a leap year is February 29
    [DataRow(2021, 60, 2021, 3, 1)]    // day 60 in a common year is March 1
    [DataRow(2020, 366, 2020, 12, 31)] // last day of a leap year
    [DataRow(2021, 365, 2021, 12, 31)] // last day of a common year
    [DataRow(2000, 366, 2000, 12, 31)] // 2000 is a leap century
    [DataRow(1900, 365, 1900, 12, 31)] // 1900 is a common century
    public void FromDayOfYear_WhenGivenOrdinalDay_ShouldReturnExpectedDate(int year, int dayOfYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);

        var actual = DateOnlyExtensions.FromDayOfYear(year, dayOfYear);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FromDayOfYear(int, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="dayOfYear" /> falls outside the valid range for
    /// the year.
    /// </summary>
    [TestMethod]
    [DataRow(2020, 0)]    // below the minimum ordinal
    [DataRow(2020, -1)]   // negative ordinal
    [DataRow(2021, 366)]  // 366 is invalid in a common year
    [DataRow(2020, 367)]  // 367 exceeds even a leap year
    public void FromDayOfYear_WhenDayOfYearOutOfRange_ShouldThrowArgumentOutOfRangeException(int year, int dayOfYear)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.FromDayOfYear(year, dayOfYear);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FromDayOfYear(int, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="year" /> is outside the supported range.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]      // year below the minimum
    [DataRow(10000, 1)]  // year above the maximum
    public void FromDayOfYear_WhenYearOutOfRange_ShouldThrowArgumentOutOfRangeException(int year, int dayOfYear)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.FromDayOfYear(year, dayOfYear);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FromDayOfYear(int, int)" /> round-trips the
    /// <see cref="DateOnly.DayOfYear" /> property for every day of a leap year and a common year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2020)] // leap year
    [DataRow(2021)] // common year
    public void FromDayOfYear_WhenRoundTrippedAcrossYear_ShouldMatchOriginalDate(int year)
    {
        var date = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        while (date <= end)
        {
            Assert.AreEqual(date, DateOnlyExtensions.FromDayOfYear(year, date.DayOfYear));
            date = date.AddDays(1);
        }
    }
}

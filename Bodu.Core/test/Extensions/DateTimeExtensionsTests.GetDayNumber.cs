// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetDayNumber.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayNumber(int, int, int)" /> returns the correct day number for representative
    /// valid year/month/day combinations.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 1)]      // first valid date
    [DataRow(2024, 1, 1)]   // common case
    [DataRow(2024, 2, 29)]  // leap day
    [DataRow(2023, 2, 28)]  // non-leap February
    [DataRow(2024, 12, 31)] // year-end
    [DataRow(9999, 12, 31)] // last valid date
    public void GetDayNumber_ForValidDate_ShouldReturnExpectedDayNumber(int year, int month, int day)
    {
        var expected = new DateOnly(year, month, day).DayNumber;
        var actual = DateTimeExtensions.GetDayNumber(year, month, day);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayNumber(int, int, int)" /> throws when the day is outside the valid range for
    /// the supplied month and year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 0)]    // day < 1
    [DataRow(2024, 1, 32)]   // day > 31
    [DataRow(2023, 2, 29)]   // Feb 29 in non-leap year
    [DataRow(2024, 4, 31)]   // April has 30 days
    public void GetDayNumber_WhenDayIsOutOfRange_ShouldThrowExactly(int year, int month, int day)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetDayNumber(year, month, day);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayNumber(int, int, int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(2024, 0, 1)]
    [DataRow(2024, 13, 1)]
    [DataRow(2024, -1, 1)]
    public void GetDayNumber_WhenMonthIsOutOfRange_ShouldThrowExactly(int year, int month, int day)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetDayNumber(year, month, day);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayNumber(int, int, int)" /> throws when the year is outside the valid range.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 1)]
    [DataRow(10000, 1, 1)]
    [DataRow(-1, 1, 1)]
    public void GetDayNumber_WhenYearIsOutOfRange_ShouldThrowExactly(int year, int month, int day)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetDayNumber(year, month, day);
        });
    }

}

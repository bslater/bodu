// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a non-working holiday, a weekend day, and an ordinary weekday are each classified correctly. In
    /// 2025, 1 January is a Wednesday holiday, 4 January is a Saturday, and 2 January is an ordinary working day.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected working-day classification.</param>
    [TestMethod]
    [DataRow(2025, 1, 1, false)]  // Wednesday holiday
    [DataRow(2025, 1, 4, false)]  // Saturday
    [DataRow(2025, 1, 2, true)]   // ordinary weekday
    public void IsWorkingDay_WhenDayIsHolidayWeekendOrWeekday_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the custom working week treats Saturday as a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WithSixDayWeek_TreatsSaturdayAsWorking()
    {
        Assert.IsTrue(new DateOnly(2025, 1, 4).IsWorkingDay(Service, "XX", WeekPattern.MondayToSaturday));
    }
}

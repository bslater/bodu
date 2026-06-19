// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.IsWeekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsWeekend(DateOnly, WeekPattern?)" /> reports Saturday and Sunday
    /// as weekend days, and a weekday as non-weekend, under the default Monday-to-Friday working week.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected weekend classification.</param>
    [TestMethod]
    [DataRow(2026, 1, 10, true)]   // Saturday is a weekend by default
    [DataRow(2026, 1, 11, true)]   // Sunday is a weekend by default
    [DataRow(2026, 1, 5, false)]   // Monday is not a weekend by default
    public void IsWeekend_WhenDefaultWorkingWeek_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWeekend());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsWeekend(DateOnly, WeekPattern?)" /> treats Saturday as a working
    /// day and Sunday as the sole weekend under a Monday-to-Saturday working week.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected weekend classification.</param>
    [TestMethod]
    [DataRow(2026, 1, 10, false)]  // Saturday is working in a six-day week
    [DataRow(2026, 1, 11, true)]   // Sunday remains a weekend
    public void IsWeekend_WhenMondayToSaturdayWeek_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWeekend(WeekPattern.MondayToSaturday));
    }
}

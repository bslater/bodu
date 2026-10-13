// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.NthWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WeekdayMathTests
{
    /// <summary>
    /// Verifies that the last-occurrence ordinal returns the final occurrence of the weekday in the month.
    /// </summary>
    [TestMethod]
    public void NthWeekdayInMonth_WhenLast_ShouldReturnFinalOccurrence()
    {
        // The last Monday of January 2025 is 2025-01-27.
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(2025, 1, DayOfWeek.Monday, WeekOrdinal.Last);

        Assert.AreEqual(new DateOnly(2025, 1, 27), result);
    }

    /// <summary>
    /// Verifies that an ordinal <see cref="WeekOrdinal" /> does not define yields no date, rather than the date of the
    /// same month in another year that adding whole weeks to the month's first match reaches: ordinal 53 lands on
    /// 1 January 2024 and ordinal -51 on 3 January 2022 for the Mondays of January 2023.
    /// </summary>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(53)]
    [DataRow(-51)]
    public void NthWeekdayInMonth_WhenOrdinalIsUndefinedAndReachesAnotherYear_ShouldReturnNull(int ordinal)
    {
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(2023, 1, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that an ordinal <see cref="WeekOrdinal" /> does not define yields no date at the ends of the date range,
    /// rather than throwing from the date arithmetic: ordinal 0 steps back before 1 January 0001, and ordinal 7 forward
    /// past 31 December 9999.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(1, 1, 0)]
    [DataRow(9999, 12, 7)]
    public void NthWeekdayInMonth_WhenOrdinalIsUndefinedAtTheEndsOfTheRange_ShouldReturnNull(int year, int month, int ordinal)
    {
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(year, month, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(result);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.NextNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the next non-working day from a working day lands on the holiday. Tuesday 31 December 2024's next
    /// non-working day is the 1 January 2025 holiday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenBeforeHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2024, 12, 31).NextNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next non-working day from a mid-week working day lands on the weekend. Thursday 2 January 2025's
    /// next non-working day is Saturday 4 January.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenMidWeek_ShouldReturnSaturday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 4), new DateOnly(2025, 1, 2).NextNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that a six-day working week treats Saturday as working, so the next non-working day is the Sunday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WithSixDayWeek_ShouldSkipSaturday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 5), new DateOnly(2025, 1, 2).NextNonWorkingDay(Service, "XX", WeekPattern.MondayToSaturday));
    }
}

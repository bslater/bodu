// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.NextNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the next non-working day from a working day before the holiday lands on the holiday. Wednesday
    /// 31 December 2025's next non-working day is the 1 January 2026 holiday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenBeforeHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31).NextNonWorkingDay(HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that the next non-working day from a mid-week working day lands on the weekend. Friday 2 January 2026's
    /// next non-working day is Saturday 3 January.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenMidWeek_ShouldReturnWeekend()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 2).NextNonWorkingDay(HolidayService, "XX"));
    }
}

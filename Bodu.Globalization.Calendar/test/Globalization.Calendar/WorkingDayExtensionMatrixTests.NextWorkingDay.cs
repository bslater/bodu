// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that the next working day after a Friday skips the weekend onto the following Monday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenFriday_ShouldReturnFollowingMonday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 2).NextWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next working day skips an intermediate non-working day. The fixture holiday on Thursday
    /// 1 January 2026 means the next working day after Wednesday 31 December 2025 is Friday 2 January 2026.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNextDayIsHoliday_ShouldSkipToFollowingWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2025, 12, 31).NextWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next working day under a Sunday-to-Thursday working week skips Friday and Saturday. From
    /// Thursday 14 May 2026 the next working day is Sunday 17 May 2026.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenSundayToThursdayWeek_ShouldSkipFridayAndSaturday()
    {
        Assert.AreEqual(new DateOnly(2026, 5, 17), new DateOnly(2026, 5, 14).NextWorkingDay(Service, "XX", WeekPattern.SundayToThursday));
    }
}

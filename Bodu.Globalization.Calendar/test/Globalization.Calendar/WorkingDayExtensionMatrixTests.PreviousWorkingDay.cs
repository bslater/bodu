// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.PreviousWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that the previous working day before a Monday skips back across the weekend to the prior Friday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenMonday_ShouldReturnPriorFriday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5).PreviousWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the previous working day skips an intermediate non-working day. From Friday 2 January 2026 the
    /// previous working day skips the 1 January holiday and the preceding weekend to Wednesday 31 December 2025.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenPriorDayIsHoliday_ShouldSkipToEarlierWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2).PreviousWorkingDay(Service, "XX"));
    }
}

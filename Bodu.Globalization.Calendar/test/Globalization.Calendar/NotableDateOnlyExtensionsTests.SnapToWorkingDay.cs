// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.SnapToWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDay" /> moves the 1 January 2025 holiday forward
    /// to the following working day, Thursday 2 January.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenHoliday_ShouldReturnFollowingWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 2), new DateOnly(2025, 1, 1).SnapToWorkingDay(Service, "XX"));
    }
}

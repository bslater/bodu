// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDayBackward" /> moves the 1 January 2025 holiday
    /// backward to the prior working day, Tuesday 31 December 2024.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenHoliday_ShouldReturnPriorWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 1).SnapToWorkingDayBackward(Service, "XX"));
    }
}

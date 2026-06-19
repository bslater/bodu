// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.PreviousWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the previous working day skips the holiday and the preceding weekend. From Thursday 2 January 2025,
    /// the previous working day skips 1 January (holiday) and the 28-29 December weekend to Tuesday 31 December 2024.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_SkipsHolidayAndWeekend()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 2).PreviousWorkingDay(Service, "XX"));
    }
}

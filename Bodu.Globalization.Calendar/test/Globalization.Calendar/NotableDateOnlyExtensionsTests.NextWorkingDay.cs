// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the next working day skips a holiday and the following weekend. 31 December 2024 is a Tuesday; the
    /// next working day skips the 1 January holiday (Wednesday) to land on Thursday 2 January 2025.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_SkipsHolidayAndWeekend()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 2), new DateOnly(2024, 12, 31).NextWorkingDay(Service, "XX"));
    }
}

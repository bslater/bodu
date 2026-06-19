// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.PreviousNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the previous non-working day from a working day lands on the holiday. Thursday 2 January 2025's
    /// previous non-working day is the 1 January holiday.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenAfterHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2).PreviousNonWorkingDay(Service, "XX"));
    }
}

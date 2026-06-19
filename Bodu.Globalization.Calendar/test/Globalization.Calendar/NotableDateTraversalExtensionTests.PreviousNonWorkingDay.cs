// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.PreviousNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the previous non-working day from a working day after the holiday lands on the holiday. Friday
    /// 2 January 2026's previous non-working day is the 1 January holiday.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenAfterHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2).PreviousNonWorkingDay(HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> previous non-working day lands on the holiday while preserving the
    /// time-of-day and kind.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime result = new DateTime(2026, 1, 2, 8, 15, 0, DateTimeKind.Local).PreviousNonWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateTime(2026, 1, 1, 8, 15, 0, DateTimeKind.Local), result);
        Assert.AreEqual(DateTimeKind.Local, result.Kind);
    }
}

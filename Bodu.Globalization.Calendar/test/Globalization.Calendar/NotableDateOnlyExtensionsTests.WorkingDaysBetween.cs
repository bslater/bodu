// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the inclusive working-day count over the first business week of 2025 excludes the holiday and the
    /// weekend.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_CountsInclusiveWorkingDays()
    {
        // 1-10 January 2025: Wed 1 (holiday), Thu 2, Fri 3, Sat 4, Sun 5, Mon 6, Tue 7, Wed 8, Thu 9, Fri 10 -> 7 working days.
        int count = new DateOnly(2025, 1, 1).WorkingDaysBetween(new DateOnly(2025, 1, 10), Service, "XX");

        Assert.AreEqual(7, count);
    }
}

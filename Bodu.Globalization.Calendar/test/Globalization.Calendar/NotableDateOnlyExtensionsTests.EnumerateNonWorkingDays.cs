// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that enumerating non-working days over the first days of 2025 yields the holiday and the weekend.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_ShouldYieldHolidayAndWeekend()
    {
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 4), new DateOnly(2025, 1, 5) },
            new DateOnly(2025, 1, 1).EnumerateNonWorkingDays(new DateOnly(2025, 1, 5), Service, "XX").ToList());
    }
}

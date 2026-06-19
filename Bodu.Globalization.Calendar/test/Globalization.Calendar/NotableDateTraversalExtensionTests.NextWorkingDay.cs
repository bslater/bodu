// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the <see cref="DateTime" /> next working day skips the holiday and weekend while preserving the
    /// time-of-day and kind. Wednesday 31 December 2025 advances to Friday 2 January 2026 (the 1 January holiday is
    /// skipped).
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime start = new(2025, 12, 31, 14, 30, 0, DateTimeKind.Utc);

        DateTime next = start.NextWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateTime(2026, 1, 2, 14, 30, 0, DateTimeKind.Utc), next);
        Assert.AreEqual(DateTimeKind.Utc, next.Kind);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> next working day skips the holiday while preserving the
    /// time-of-day and offset.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_OnDateTimeOffset_ShouldPreserveTimeAndOffset()
    {
        DateTimeOffset start = new(2025, 12, 31, 14, 30, 0, TimeSpan.FromHours(5));

        DateTimeOffset next = start.NextWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateTimeOffset(2026, 1, 2, 14, 30, 0, TimeSpan.FromHours(5)), next);
        Assert.AreEqual(TimeSpan.FromHours(5), next.Offset);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that <see cref="DateTime" /> signed working-day arithmetic preserves the time-of-day and kind across the
    /// holiday and weekend.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime start = new(2025, 12, 31, 6, 0, 0, DateTimeKind.Unspecified);

        DateTime result = start.AddWorkingDays(3, HolidayService, "XX");

        Assert.AreEqual(new DateTime(2026, 1, 6, 6, 0, 0, DateTimeKind.Unspecified), result);
        Assert.AreEqual(DateTimeKind.Unspecified, result.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeOffset" /> signed working-day arithmetic preserves the time-of-day and offset.
    /// From a Friday it advances one working day onto the following Monday in the same offset.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_OnDateTimeOffset_ShouldPreserveTimeAndOffset()
    {
        // 2026-05-15 is a Friday.
        DateTimeOffset friday = new(2026, 5, 15, 9, 30, 0, TimeSpan.FromHours(-8));

        DateTimeOffset monday = friday.AddWorkingDays(1, HolidayService, "XX");

        Assert.AreEqual(new DateTimeOffset(2026, 5, 18, 9, 30, 0, TimeSpan.FromHours(-8)), monday);
        Assert.AreEqual(TimeSpan.FromHours(-8), monday.Offset);
    }
}

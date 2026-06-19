// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that enumerating non-working days over the first business week of 2026 yields the Saturday and Sunday.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenOneWeekRange_ShouldYieldWeekend()
    {
        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is included by the non-working-day enumeration. The holiday
    /// on Thursday 1 January 2026 is yielded alongside the surrounding weekend.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenHolidayInRange_ShouldIncludeIt()
    {
        DateOnly[] result = new DateOnly(2025, 12, 31).EnumerateNonWorkingDays(new DateOnly(2026, 1, 5), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4) },
            result);
    }

    /// <summary>
    /// Verifies the non-working-day yield for a single-day range across weekdays and weekend days.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected number of non-working days yielded.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SingleDayNonWorkingYieldRows))]
    public void EnumerateNonWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(int year, int month, int day, int expected)
    {
        DateOnly d = new(year, month, day);

        Assert.HasCount(expected, d.EnumerateNonWorkingDays(d, HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that enumerating <see cref="DateTime" /> non-working days reattaches the start's time-of-day and kind to
    /// each yielded value.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_OnDateTime_ShouldReattachStartTimeAndKind()
    {
        var days = new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc)
            .EnumerateNonWorkingDays(new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc), HolidayService, "XX")
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 3, 6, 15, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 4, 6, 15, 0, DateTimeKind.Utc),
            },
            days);
        Assert.IsTrue(days.TrueForAll(d => d.Kind == DateTimeKind.Utc));
    }
}

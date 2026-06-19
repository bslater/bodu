// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.EnumerateWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that enumerating working days over the first business week of 2026 yields the five weekdays in ascending
    /// order.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenOneWeekRange_ShouldYieldFiveWeekdays()
    {
        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 6),
                new DateOnly(2026, 1, 7),
                new DateOnly(2026, 1, 8),
                new DateOnly(2026, 1, 9),
            },
            result);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is skipped by the working-day enumeration. With the holiday
    /// on Thursday 1 January 2026, enumerating 31 December 2025 to 2 January 2026 yields only the two weekday working
    /// days.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenHolidayInRange_ShouldSkipIt()
    {
        DateOnly[] result = new DateOnly(2025, 12, 31).EnumerateWorkingDays(new DateOnly(2026, 1, 2), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2) },
            result);
    }

    /// <summary>
    /// Verifies the working-day yield for a single-day range across weekdays and weekend days.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected number of working days yielded.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SingleDayWorkingYieldRows))]
    public void EnumerateWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(int year, int month, int day, int expected)
    {
        DateOnly d = new(year, month, day);

        Assert.HasCount(expected, d.EnumerateWorkingDays(d, HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that reversed boundaries yield an empty working-day sequence under v2 ascending-only enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenBoundariesReversed_ShouldYieldNothing()
    {
        DateOnly[] reversed = new DateOnly(2026, 1, 11).EnumerateWorkingDays(new DateOnly(2026, 1, 5), HolidayService, "XX").ToArray();

        Assert.IsEmpty(reversed);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly from the
    /// working-day enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), null!, "XX");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTime" />-based working-day enumeration yields the working days in the range while
    /// preserving the start's time-of-day and kind.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime start = new(2026, 1, 5, 9, 30, 0, DateTimeKind.Utc);
        DateTime end = new(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc);

        DateTime[] result = start.EnumerateWorkingDays(end, HolidayService, "XX").ToArray();

        Assert.HasCount(5, result);
        Assert.AreEqual(new DateTime(2026, 1, 5, 9, 30, 0, DateTimeKind.Utc), result[0]);
        Assert.AreEqual(DateTimeKind.Utc, result[0].Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeOffset" />-based working-day enumeration yields the working days in the range
    /// while preserving the start's offset and time-of-day.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_OnDateTimeOffset_ShouldPreserveOffsetAndTime()
    {
        var offset = TimeSpan.FromHours(10);
        DateTimeOffset start = new(2026, 1, 5, 9, 30, 0, offset);
        DateTimeOffset end = new(2026, 1, 9, 0, 0, 0, offset);

        DateTimeOffset[] result = start.EnumerateWorkingDays(end, HolidayService, "XX").ToArray();

        Assert.HasCount(5, result);
        Assert.AreEqual(offset, result[0].Offset);
        Assert.AreEqual(new TimeOnly(9, 30), TimeOnly.FromDateTime(result[0].DateTime));
    }
}

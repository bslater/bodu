// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

[TestClass]
public class NotableDateTimeOffsetExtensionsTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static NotableDateService BuildService() => new();

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.IsWorkingDay" /> returns <see langword="false" /> on a
    /// Saturday civil date when the service uses the default Mon-Fri working week.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSaturdayInUtc_ShouldReturnFalseUnderMondayToFriday()
    {
        NotableDateService service = BuildService();
        // 2026-05-16 is a Saturday.
        var saturday = new DateTimeOffset(2026, 5, 16, 9, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(saturday.IsWorkingDay(Utc, service));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.IsWorkingDay" /> with an explicit Sun-Thu working week
    /// treats Friday as non-working.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenFridayAndSundayToThursdayPattern_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();
        var friday = new DateTimeOffset(2026, 5, 15, 9, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(friday.IsWorkingDay(Utc, service, WeekPattern.SundayToThursday));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.AddWorkingDays" /> advances by one working day under the
    /// default Mon-Fri week, returning a result anchored in the same time zone with the same time-of-day.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenFridayPlusOneAndDefaultWeek_ShouldReturnMonday()
    {
        NotableDateService service = BuildService();
        // 2026-05-15 is a Friday.
        var friday = new DateTimeOffset(2026, 5, 15, 9, 30, 0, TimeSpan.Zero);

        DateTimeOffset monday = friday.AddWorkingDays(Utc, service, 1);

        Assert.AreEqual(new DateOnly(2026, 5, 18), DateOnly.FromDateTime(monday.DateTime));
        Assert.AreEqual(new TimeSpan(9, 30, 0), monday.TimeOfDay);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.AddWorkingDays" /> with zero days returns the input.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenDaysIsZero_ShouldReturnInput()
    {
        NotableDateService service = BuildService();
        var input = new DateTimeOffset(2026, 5, 15, 9, 30, 0, TimeSpan.Zero);

        DateTimeOffset result = input.AddWorkingDays(Utc, service, 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.WorkingDaysBetween" /> counts working days inclusively
    /// across the civil-date interval in the supplied zone.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenMondayToFridayDefaultWeek_ShouldReturnFive()
    {
        NotableDateService service = BuildService();
        // Mon 2026-05-11 → Fri 2026-05-15 → 5 working days inclusive.
        var monday = new DateTimeOffset(2026, 5, 11, 0, 0, 0, TimeSpan.Zero);
        var friday = new DateTimeOffset(2026, 5, 15, 23, 59, 0, TimeSpan.Zero);

        var count = monday.WorkingDaysBetween(friday, Utc, service);

        Assert.AreEqual(5, count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeOffsetExtensions.IsWorkingDay" /> throws when <paramref name="timeZone" />
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenTimeZoneIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();
        DateTimeOffset instant = DateTimeOffset.UtcNow;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = instant.IsWorkingDay(null!, service);
        });
    }
}

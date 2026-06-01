// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NextWeekday.WeekPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WeekPattern)" /> returns the next selected
    /// day with a non-standard working pattern (e.g. only Wednesday selected).
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWorkingWeekIsCustom_ShouldAdvanceToNextSelectedDay()
    {
        WeekPattern pattern = new(DayOfWeek.Wednesday);

        // Mon 15 Apr 2024 → next Wednesday = Wed 17 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 15).NextWeekday(pattern));
        // Wed 17 Apr 2024 → next selected (Wed) → Wed 24 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 24), new DateOnly(2024, 4, 17).NextWeekday(pattern));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WeekPattern)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when supplied an empty working-week pattern.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWorkingWeekIsEmpty_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextWeekday(WeekPattern.Empty);
        });
    }
    // =========================================================================
    // NextWeekday(this DateOnly, WeekPattern)
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WeekPattern)" /> returns the next day
    /// whose <see cref="DayOfWeek" /> is selected by the supplied <see cref="WeekPattern" /> pattern, when the
    /// pattern matches the standard Monday-through-Friday working week.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWorkingWeekIsMondayThroughFriday_ShouldSkipSaturdayAndSunday()
    {
        WeekPattern pattern = WeekPattern.MondayToFriday;

        // Fri 19 Apr 2024 → next selected day skips Sat/Sun → Mon 22 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 22), new DateOnly(2024, 4, 19).NextWeekday(pattern));
        // Sat 20 Apr 2024 → Mon 22 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 22), new DateOnly(2024, 4, 20).NextWeekday(pattern));
        // Mon 22 Apr 2024 → Tue 23 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 23), new DateOnly(2024, 4, 22).NextWeekday(pattern));
    }

}

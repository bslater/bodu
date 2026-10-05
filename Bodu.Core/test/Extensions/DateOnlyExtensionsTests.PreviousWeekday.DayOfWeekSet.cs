// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousWeekday.DayOfWeekSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, DayOfWeekSet)" /> returns the previous
    /// selected day with a non-standard working week (e.g. only Wednesday selected).
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsCustom_ShouldAdvanceToPreviousSelectedDay()
    {
        DayOfWeekSet days = new(DayOfWeek.Wednesday);

        // Fri 19 Apr 2024 → previous Wednesday = Wed 17 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 19).PreviousWeekday(days));
        // Wed 17 Apr 2024 → previous selected (Wed) → Wed 10 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 10), new DateOnly(2024, 4, 17).PreviousWeekday(days));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, DayOfWeekSet)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when supplied an empty working week.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsEmpty_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday(DayOfWeekSet.Empty);
        });
    }
    // =========================================================================
    // PreviousWeekday(this DateOnly, DayOfWeekSet)
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, DayOfWeekSet)" /> returns the previous
    /// day whose <see cref="DayOfWeek" /> is selected by the supplied <see cref="DayOfWeekSet" />, when
    /// the set matches the standard Monday-through-Friday working week.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsMondayThroughFriday_ShouldSkipSaturdayAndSunday()
    {
        DayOfWeekSet days = DayOfWeekSet.MondayToFriday;

        // Mon 22 Apr 2024 → previous selected day skips Sun/Sat → Fri 19 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 19), new DateOnly(2024, 4, 22).PreviousWeekday(days));
        // Sun 21 Apr 2024 → Fri 19 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 19), new DateOnly(2024, 4, 21).PreviousWeekday(days));
        // Fri 19 Apr 2024 → Thu 18 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 18), new DateOnly(2024, 4, 19).PreviousWeekday(days));
    }

}

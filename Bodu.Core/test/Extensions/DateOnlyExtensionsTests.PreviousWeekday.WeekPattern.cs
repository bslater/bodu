// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousWeekday.WeekPattern.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WeekPattern)" /> returns the previous
    /// selected day with a non-standard working pattern (e.g. only Wednesday selected).
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsCustom_ShouldAdvanceToPreviousSelectedDay()
    {
        WeekPattern pattern = new(DayOfWeek.Wednesday);

        // Fri 19 Apr 2024 → previous Wednesday = Wed 17 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 19).PreviousWeekday(pattern));
        // Wed 17 Apr 2024 → previous selected (Wed) → Wed 10 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 10), new DateOnly(2024, 4, 17).PreviousWeekday(pattern));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WeekPattern)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when supplied an empty working-week pattern.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsEmpty_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday(WeekPattern.Empty);
        });
    }
    // =========================================================================
    // PreviousWeekday(this DateOnly, WeekPattern)
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WeekPattern)" /> returns the previous
    /// day whose <see cref="DayOfWeek" /> is selected by the supplied <see cref="WeekPattern" /> pattern, when
    /// the pattern matches the standard Monday-through-Friday working week.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsMondayThroughFriday_ShouldSkipSaturdayAndSunday()
    {
        WeekPattern pattern = WeekPattern.MondayToFriday;

        // Mon 22 Apr 2024 → previous selected day skips Sun/Sat → Fri 19 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 19), new DateOnly(2024, 4, 22).PreviousWeekday(pattern));
        // Sun 21 Apr 2024 → Fri 19 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 19), new DateOnly(2024, 4, 21).PreviousWeekday(pattern));
        // Fri 19 Apr 2024 → Thu 18 Apr.
        Assert.AreEqual(new DateOnly(2024, 4, 18), new DateOnly(2024, 4, 19).PreviousWeekday(pattern));
    }

}

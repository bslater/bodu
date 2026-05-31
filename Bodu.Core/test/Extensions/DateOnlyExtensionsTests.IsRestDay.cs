// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsRestDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WeekPattern)" /> returns
    /// <see langword="false" /> when the date's day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayInPattern_ShouldReturnFalse()
    {
        var monday = new DateOnly(2026, 5, 11);

        Assert.IsFalse(monday.IsRestDay(WeekPattern.Weekdays));
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WeekPattern)" /> returns
    /// <see langword="true" /> when the date's day-of-week is not selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayNotInPattern_ShouldReturnTrue()
    {
        // 2026-05-16 is a Saturday.
        var saturday = new DateOnly(2026, 5, 16);

        Assert.IsTrue(saturday.IsRestDay(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WorkingDaysOfWeek)" /> agrees with the
    /// <see cref="WeekPattern" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchWeekPatternOverload()
    {
        var friday = new DateOnly(2026, 5, 15);

        Assert.IsTrue(friday.IsRestDay(WorkingDaysOfWeek.SundayToThursday));
        Assert.IsFalse(friday.IsRestDay(WorkingDaysOfWeek.MondayToFriday));
    }

}

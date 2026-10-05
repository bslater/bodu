// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsRestDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, DayOfWeekSet)" /> returns
    /// <see langword="false" /> when the date's day-of-week is selected in the supplied set.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayInSet_ShouldReturnFalse()
    {
        var monday = new DateOnly(2026, 5, 11);

        Assert.IsFalse(monday.IsRestDay(DayOfWeekSet.Weekdays));
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, DayOfWeekSet)" /> returns
    /// <see langword="true" /> when the date's day-of-week is not selected in the supplied set.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayNotInSet_ShouldReturnTrue()
    {
        // 2026-05-16 is a Saturday.
        var saturday = new DateOnly(2026, 5, 16);

        Assert.IsTrue(saturday.IsRestDay(DayOfWeekSet.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WorkingDaysOfWeek)" /> agrees with the
    /// <see cref="DayOfWeekSet" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchDayOfWeekSetOverload()
    {
        var friday = new DateOnly(2026, 5, 15);

        Assert.IsTrue(friday.IsRestDay(WorkingDaysOfWeek.SundayToThursday));
        Assert.IsFalse(friday.IsRestDay(WorkingDaysOfWeek.MondayToFriday));
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsInWorkingWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, DayOfWeekSet)" /> returns
    /// <see langword="true" /> when the date's day-of-week is selected in the supplied set.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayInSet_ShouldReturnTrue()
    {
        // 2026-05-11 is a Monday.
        var monday = new DateOnly(2026, 5, 11);

        Assert.IsTrue(monday.IsInWorkingWeek(DayOfWeekSet.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, DayOfWeekSet)" /> returns
    /// <see langword="false" /> when the date's day-of-week is not selected in the supplied set.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayNotInSet_ShouldReturnFalse()
    {
        // 2026-05-16 is a Saturday.
        var saturday = new DateOnly(2026, 5, 16);

        Assert.IsFalse(saturday.IsInWorkingWeek(DayOfWeekSet.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, WorkingDaysOfWeek)" /> agrees
    /// with the <see cref="DayOfWeekSet" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchDayOfWeekSetOverload()
    {
        var friday = new DateOnly(2026, 5, 15);

        Assert.IsFalse(friday.IsInWorkingWeek(WorkingDaysOfWeek.SundayToThursday));
        Assert.IsTrue(friday.IsInWorkingWeek(WorkingDaysOfWeek.MondayToFriday));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, WorkingDaysOfWeek)" /> throws
    /// <see cref="ArgumentException" /> when called with <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenWorkingDaysOfWeekIsCustom_ShouldThrowExactly()
    {
        var date = new DateOnly(2026, 5, 11);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = date.IsInWorkingWeek(WorkingDaysOfWeek.Custom);
        });
    }

}

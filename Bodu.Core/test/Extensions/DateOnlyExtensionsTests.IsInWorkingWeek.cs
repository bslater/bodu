// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsInWorkingWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, WeekPattern)" /> returns
    /// <see langword="true" /> when the date's day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayInPattern_ShouldReturnTrue()
    {
        // 2026-05-11 is a Monday.
        DateOnly monday = new DateOnly(2026, 5, 11);

        Assert.IsTrue(monday.IsInWorkingWeek(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, WeekPattern)" /> returns
    /// <see langword="false" /> when the date's day-of-week is not selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayNotInPattern_ShouldReturnFalse()
    {
        // 2026-05-16 is a Saturday.
        DateOnly saturday = new DateOnly(2026, 5, 16);

        Assert.IsFalse(saturday.IsInWorkingWeek(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, WorkingDaysOfWeek)" /> agrees
    /// with the <see cref="WeekPattern" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchWeekPatternOverload()
    {
        DateOnly friday = new DateOnly(2026, 5, 15);

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
        DateOnly date = new DateOnly(2026, 5, 11);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = date.IsInWorkingWeek(WorkingDaysOfWeek.Custom);
        });
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsRestDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WeekPattern)" /> returns
    /// <see langword="true" /> when the date's day-of-week is not selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayNotInPattern_ShouldReturnTrue()
    {
        // 2026-05-16 is a Saturday.
        DateOnly saturday = new DateOnly(2026, 5, 16);

        Assert.IsTrue(saturday.IsRestDay(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WeekPattern)" /> returns
    /// <see langword="false" /> when the date's day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenDayInPattern_ShouldReturnFalse()
    {
        DateOnly monday = new DateOnly(2026, 5, 11);

        Assert.IsFalse(monday.IsRestDay(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsRestDay(DateOnly, WorkingDaysOfWeek)" /> agrees with the
    /// <see cref="WeekPattern" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsRestDay_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchWeekPatternOverload()
    {
        DateOnly friday = new DateOnly(2026, 5, 15);

        Assert.IsTrue(friday.IsRestDay(WorkingDaysOfWeek.SundayToThursday));
        Assert.IsFalse(friday.IsRestDay(WorkingDaysOfWeek.MondayToFriday));
    }
}

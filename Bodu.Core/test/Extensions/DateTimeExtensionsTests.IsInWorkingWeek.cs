// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsInWorkingWeek.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInWorkingWeek(DateTime, WeekPattern)" /> returns
    /// <see langword="true" /> when the date's day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayInPattern_ShouldReturnTrue()
    {
        var monday = new DateTime(2026, 5, 11);

        Assert.IsTrue(monday.IsInWorkingWeek(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInWorkingWeek(DateTime, WeekPattern)" /> returns
    /// <see langword="false" /> when the date's day-of-week is not selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenDayNotInPattern_ShouldReturnFalse()
    {
        var saturday = new DateTime(2026, 5, 16);

        Assert.IsFalse(saturday.IsInWorkingWeek(WeekPattern.Weekdays));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInWorkingWeek(DateTime, WorkingDaysOfWeek)" /> agrees with the
    /// <see cref="WeekPattern" /> overload for a named preset.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenUsingWorkingDaysOfWeekSugar_ShouldMatchWeekPatternOverload()
    {
        var friday = new DateTime(2026, 5, 15);

        Assert.IsFalse(friday.IsInWorkingWeek(WorkingDaysOfWeek.SundayToThursday));
        Assert.IsTrue(friday.IsInWorkingWeek(WorkingDaysOfWeek.MondayToFriday));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInWorkingWeek(DateTime, WorkingDaysOfWeek)" /> throws
    /// <see cref="ArgumentException" /> when called with <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    [TestMethod]
    public void IsInWorkingWeek_WhenWorkingDaysOfWeekIsCustom_ShouldThrowExactly()
    {
        var date = new DateTime(2026, 5, 11);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = date.IsInWorkingWeek(WorkingDaysOfWeek.Custom);
        });
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NextWeekdayDayOfWeekSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, DayOfWeekSet)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied pattern is empty.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWorkingWeekIsEmpty_ShouldThrowExactly()
    {
        var monday = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = monday.NextWeekday(DayOfWeekSet.Empty);
        });
    }
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, DayOfWeekSet)" /> returns the first day after
    /// the input whose day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWorkingWeekIsMondayToFriday_ShouldSkipWeekend()
    {
        // 2024-04-19 is a Friday.
        var friday = new DateTime(2024, 4, 19);

        DateTime actual = friday.NextWeekday(DayOfWeekSet.MondayToFriday);

        Assert.AreEqual(new DateTime(2024, 4, 22), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, DayOfWeekSet)" /> with a Sunday-to-Thursday
    /// pattern lands on Sunday when starting on Thursday (Friday and Saturday are non-working).
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekPatternIsSundayToThursday_ShouldSkipFridayAndSaturday()
    {
        // 2024-04-18 is a Thursday.
        var thursday = new DateTime(2024, 4, 18);

        DateTime actual = thursday.NextWeekday(DayOfWeekSet.SundayToThursday);

        Assert.AreEqual(new DateTime(2024, 4, 21), actual); // Sunday
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, DayOfWeekSet)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied pattern is empty.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWorkingWeekIsEmpty_ShouldThrowExactly()
    {
        var monday = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = monday.PreviousWeekday(DayOfWeekSet.Empty);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, DayOfWeekSet)" /> returns the first day
    /// before the input whose day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekPatternIsMondayToFriday_ShouldSkipWeekend()
    {
        // 2024-04-22 is a Monday.
        var monday = new DateTime(2024, 4, 22);

        DateTime actual = monday.PreviousWeekday(DayOfWeekSet.MondayToFriday);

        Assert.AreEqual(new DateTime(2024, 4, 19), actual); // Friday
    }

}

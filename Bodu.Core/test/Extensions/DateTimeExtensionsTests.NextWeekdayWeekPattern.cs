// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NextWeekdayWeekPattern.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WeekPattern)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied pattern is empty.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekPatternIsEmpty_ShouldThrowExactly()
    {
        var monday = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = monday.NextWeekday(WeekPattern.Empty);
        });
    }
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WeekPattern)" /> returns the first day after
    /// the input whose day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekPatternIsMondayToFriday_ShouldSkipWeekend()
    {
        // 2024-04-19 is a Friday.
        var friday = new DateTime(2024, 4, 19);

        DateTime actual = friday.NextWeekday(WeekPattern.MondayToFriday);

        Assert.AreEqual(new DateTime(2024, 4, 22), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WeekPattern)" /> with a Sunday-to-Thursday
    /// pattern lands on Sunday when starting on Thursday (Friday and Saturday are non-working).
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekPatternIsSundayToThursday_ShouldSkipFridayAndSaturday()
    {
        // 2024-04-18 is a Thursday.
        var thursday = new DateTime(2024, 4, 18);

        DateTime actual = thursday.NextWeekday(WeekPattern.SundayToThursday);

        Assert.AreEqual(new DateTime(2024, 4, 21), actual); // Sunday
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, WeekPattern)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied pattern is empty.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekPatternIsEmpty_ShouldThrowExactly()
    {
        var monday = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = monday.PreviousWeekday(WeekPattern.Empty);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, WeekPattern)" /> returns the first day
    /// before the input whose day-of-week is selected in the supplied pattern.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekPatternIsMondayToFriday_ShouldSkipWeekend()
    {
        // 2024-04-22 is a Monday.
        var monday = new DateTime(2024, 4, 22);

        DateTime actual = monday.PreviousWeekday(WeekPattern.MondayToFriday);

        Assert.AreEqual(new DateTime(2024, 4, 19), actual); // Friday
    }

}

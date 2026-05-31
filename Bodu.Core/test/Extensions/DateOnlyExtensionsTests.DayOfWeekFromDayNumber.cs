// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.DayOfWeekFromDayNumber.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetNextDayOfWeekFromDayNumber" /> returns the forward delta to reach the next
    /// occurrence of the requested <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void GetNextDayOfWeekFromDayNumber_ShouldReturnForwardDelta()
    {
        var monday = new DateOnly(2024, 1, 1); // Monday → day number known
        Assert.AreEqual(0, DateOnlyExtensions.GetNextDayOfWeekFromDayNumber(monday.DayNumber, DayOfWeek.Monday));
        Assert.AreEqual(1, DateOnlyExtensions.GetNextDayOfWeekFromDayNumber(monday.DayNumber, DayOfWeek.Tuesday));
        Assert.AreEqual(5, DateOnlyExtensions.GetNextDayOfWeekFromDayNumber(monday.DayNumber, DayOfWeek.Saturday));
        Assert.AreEqual(6, DateOnlyExtensions.GetNextDayOfWeekFromDayNumber(monday.DayNumber, DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetPreviousDayOfWeekFromDayNumber" /> returns a negative delta to reach the previous
    /// occurrence of the requested <see cref="DayOfWeek" />, and returns -7 when the current day already matches the request.
    /// </summary>
    [TestMethod]
    public void GetPreviousDayOfWeekFromDayNumber_ShouldReturnBackwardDelta()
    {
        var wednesday = new DateOnly(2024, 1, 3); // Wednesday
        Assert.AreEqual(-1, DateOnlyExtensions.GetPreviousDayOfWeekFromDayNumber(wednesday.DayNumber, DayOfWeek.Tuesday));
        Assert.AreEqual(-2, DateOnlyExtensions.GetPreviousDayOfWeekFromDayNumber(wednesday.DayNumber, DayOfWeek.Monday));
        Assert.AreEqual(-7, DateOnlyExtensions.GetPreviousDayOfWeekFromDayNumber(wednesday.DayNumber, DayOfWeek.Wednesday));
        Assert.AreEqual(-3, DateOnlyExtensions.GetPreviousDayOfWeekFromDayNumber(wednesday.DayNumber, DayOfWeek.Sunday));
    }

}

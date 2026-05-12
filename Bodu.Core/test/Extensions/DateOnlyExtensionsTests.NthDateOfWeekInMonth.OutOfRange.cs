// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NthDateOfWeekInMonth.OutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetNthDateOfWeekInMonth(int, int, DayOfWeek, WeekOfMonthOrdinal)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the requested ordinal does not exist in the supplied month.
    /// </summary>
    [TestMethod]
    public void GetNthDateOfWeekInMonth_DateOnly_WhenOrdinalDoesNotExistForMonth_ShouldThrowExactly()
    {
        // February 2023 only has 4 Mondays; requesting the fifth must throw.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetNthDateOfWeekInMonth(2023, 2, DayOfWeek.Monday, WeekOfMonthOrdinal.Fifth);
        });
    }
}

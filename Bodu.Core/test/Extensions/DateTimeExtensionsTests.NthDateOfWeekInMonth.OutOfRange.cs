// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NthDateOfWeekInMonth.OutOfRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetNthDateOfWeekInMonth(int, int, DayOfWeek, WeekOfMonthOrdinal)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the requested ordinal does not exist in the supplied month.
    /// </summary>
    [TestMethod]
    public void GetNthDateOfWeekInMonth_DateTime_WhenOrdinalDoesNotExistForMonth_ShouldThrowExactly()
    {
        // February 2023 only has 4 Mondays; requesting the fifth must throw.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNthDateOfWeekInMonth(2023, 2, DayOfWeek.Monday, WeekOfMonthOrdinal.Fifth);
        });
    }

}

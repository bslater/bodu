// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NthDateOfWeekInMonth.OutOfRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetNthDateOfWeekInMonth(int, int, DayOfWeek, WeekOrdinal)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the requested ordinal does not exist in the supplied month.
    /// </summary>
    [TestMethod]
    public void GetNthDateOfWeekInMonth_DateTime_WhenOrdinalDoesNotExistForMonth_ShouldThrowExactly()
    {
        // February 2023 only has 4 Mondays; requesting the fifth must throw.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNthDateOfWeekInMonth(2023, 2, DayOfWeek.Monday, WeekOrdinal.Fifth);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetNthDateOfWeekInMonth(int, int, DayOfWeek, WeekOrdinal)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>ordinal</c>, with the resource message, for a fifth
    /// occurrence December 9999 lacks, as in any other month.
    /// </summary>
    /// <param name="dayOfWeek">A weekday that occurs only four times in December 9999.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday)]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    public void GetNthDateOfWeekInMonth_DateTime_WhenFifthDoesNotExistInDecember9999_ShouldThrowForOrdinal(DayOfWeek dayOfWeek)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNthDateOfWeekInMonth(9999, 12, dayOfWeek, WeekOrdinal.Fifth);
        });

        string expected = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, WeekOrdinal.Fifth, dayOfWeek, new DateTime(9999, 12, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture));
        Assert.AreEqual("ordinal", ex.ParamName);
        Assert.StartsWith(expected, ex.Message);
    }

}

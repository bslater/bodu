// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekInMonthStrategyTests.Calculate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DayOfWeekInMonthStrategyTests
{
    /// <summary>
    /// Verifies that a strategy asking for a fifth occurrence December 9999 does not have yields no date for that
    /// year, rather than throwing from the date arithmetic.
    /// </summary>
    /// <param name="dayOfWeek">The weekday with only four occurrences in December 9999.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday)]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    public void Calculate_WhenAMissingFifthWouldFallAfterDecember9999_ShouldReturnNull(DayOfWeek dayOfWeek)
    {
        var sut = new DayOfWeekInMonthStrategy(12, dayOfWeek, WeekOrdinal.Fifth);

        Assert.IsNull(sut.Calculate(9999, CreateContext()));
    }
}

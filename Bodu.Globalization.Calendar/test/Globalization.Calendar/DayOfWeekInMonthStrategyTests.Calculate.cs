// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekInMonthStrategyTests.Calculate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DayOfWeekInMonthStrategyTests
{
    /// <summary>
    /// Verifies that a strategy built with an ordinal <see cref="WeekOrdinal" /> does not define yields no date for a
    /// year, rather than a date in another year: ordinal 53 reaches 1 January 2024 from the Mondays of January 2023.
    /// </summary>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(53)]
    [DataRow(-51)]
    public void Calculate_WhenOrdinalIsUndefinedAndReachesAnotherYear_ShouldReturnNull(int ordinal)
    {
        var sut = new DayOfWeekInMonthStrategy(1, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(sut.Calculate(2023, CreateContext()));
    }

    /// <summary>
    /// Verifies that a strategy built with an ordinal <see cref="WeekOrdinal" /> does not define yields no date at the
    /// ends of the date range, rather than throwing from the date arithmetic.
    /// </summary>
    /// <param name="year">The year to calculate.</param>
    /// <param name="month">The strategy's month.</param>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(1, 1, 0)]
    [DataRow(9999, 12, 7)]
    public void Calculate_WhenOrdinalIsUndefinedAtTheEndsOfTheRange_ShouldReturnNull(int year, int month, int ordinal)
    {
        var sut = new DayOfWeekInMonthStrategy(month, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(sut.Calculate(year, CreateContext()));
    }

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

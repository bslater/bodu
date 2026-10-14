// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RelativeWeekdayInMonthStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class RelativeWeekdayInMonthStrategyTests
{
    /// <summary>
    /// Verifies that a month outside 1 to 12, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" />
    /// naming the <c>month</c> parameter.
    /// </summary>
    /// <param name="month">The month.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void Ctor_WhenMonthIsOutsideOneToTwelve_ShouldThrowArgumentOutOfRangeException(int month)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new RelativeWeekdayInMonthStrategy(month, DayOfWeek.Monday, WeekOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.After);
        });

        Assert.AreEqual("month", ex.ParamName);
    }
}

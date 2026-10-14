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

    /// <summary>
    /// Verifies that an enumeration value its type does not define, which the loader rejects with <c>BODU-CAL-ENUM</c>,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the parameter, as the binary pack reader rejects it.
    /// </summary>
    /// <param name="parameter">The name of the parameter given the undefined value.</param>
    [TestMethod]
    [DataRow("dayOfWeek")]
    [DataRow("weekOrdinal")]
    [DataRow("relativeDayOfWeek")]
    [DataRow("direction")]
    public void Ctor_WhenEnumValueIsUndefined_ShouldThrowArgumentOutOfRangeException(string parameter)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = parameter switch
            {
                "dayOfWeek" => new RelativeWeekdayInMonthStrategy(5, (DayOfWeek)7, WeekOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.After),
                "weekOrdinal" => new RelativeWeekdayInMonthStrategy(5, DayOfWeek.Monday, (WeekOrdinal)99, DayOfWeek.Tuesday, WeekdayProximity.After),
                "relativeDayOfWeek" => new RelativeWeekdayInMonthStrategy(5, DayOfWeek.Monday, WeekOrdinal.First, (DayOfWeek)7, WeekdayProximity.After),
                "direction" => new RelativeWeekdayInMonthStrategy(5, DayOfWeek.Monday, WeekOrdinal.First, DayOfWeek.Tuesday, (WeekdayProximity)99),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }
}

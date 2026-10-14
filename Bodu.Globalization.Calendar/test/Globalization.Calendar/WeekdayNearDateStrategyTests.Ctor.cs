// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearDateStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WeekdayNearDateStrategyTests
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
            _ = new WeekdayNearDateStrategy(month, 1, DayOfWeek.Monday, WeekdayProximity.After);
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a day outside 1 to 31, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" /> naming
    /// the <c>day</c> parameter.
    /// </summary>
    /// <param name="day">The day.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(32)]
    public void Ctor_WhenDayIsOutsideOneToThirtyOne_ShouldThrowArgumentOutOfRangeException(int day)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new WeekdayNearDateStrategy(5, day, DayOfWeek.Monday, WeekdayProximity.After);
        });

        Assert.AreEqual("day", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an enumeration value its type does not define, which the loader rejects with <c>BODU-CAL-ENUM</c>,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the parameter, as the binary pack reader rejects it.
    /// </summary>
    /// <param name="parameter">The name of the parameter given the undefined value.</param>
    [TestMethod]
    [DataRow("dayOfWeek")]
    [DataRow("direction")]
    public void Ctor_WhenEnumValueIsUndefined_ShouldThrowArgumentOutOfRangeException(string parameter)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = parameter switch
            {
                "dayOfWeek" => new WeekdayNearDateStrategy(5, 1, (DayOfWeek)7, WeekdayProximity.After),
                "direction" => new WeekdayNearDateStrategy(5, 1, DayOfWeek.Monday, (WeekdayProximity)99),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekInMonthStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DayOfWeekInMonthStrategyTests
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
            _ = new DayOfWeekInMonthStrategy(month, DayOfWeek.Monday, WeekOrdinal.First);
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
    public void Ctor_WhenEnumValueIsUndefined_ShouldThrowArgumentOutOfRangeException(string parameter)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = parameter switch
            {
                "dayOfWeek" => new DayOfWeekInMonthStrategy(5, (DayOfWeek)7, WeekOrdinal.First),
                "weekOrdinal" => new DayOfWeekInMonthStrategy(5, DayOfWeek.Monday, (WeekOrdinal)99),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }

    /// <summary>
    /// Verifies that a week ordinal <see cref="WeekOrdinal" /> does not define throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>weekOrdinal</c> parameter, rather than constructing a
    /// strategy that reaches a date in another month or year: these ordinals were the ones the calculation once had to
    /// guard against.
    /// </summary>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(53)]
    [DataRow(-51)]
    [DataRow(0)]
    [DataRow(7)]
    public void Ctor_WhenWeekOrdinalIsUndefined_ShouldThrowArgumentOutOfRangeException(int ordinal)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DayOfWeekInMonthStrategy(1, DayOfWeek.Monday, (WeekOrdinal)ordinal);
        });

        Assert.AreEqual("weekOrdinal", ex.ParamName);
    }
}

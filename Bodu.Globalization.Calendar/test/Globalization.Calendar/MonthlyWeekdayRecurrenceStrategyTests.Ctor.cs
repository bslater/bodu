// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthlyWeekdayRecurrenceStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class MonthlyWeekdayRecurrenceStrategyTests
{
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
                "dayOfWeek" => new MonthlyWeekdayRecurrenceStrategy((DayOfWeek)7, WeekOrdinal.First),
                "weekOrdinal" => new MonthlyWeekdayRecurrenceStrategy(DayOfWeek.Monday, (WeekOrdinal)99),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an interval below 1, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" />
    /// naming the <c>intervalMonths</c> parameter.
    /// </summary>
    /// <param name="interval">The interval.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenIntervalIsBelowOne_ShouldThrowArgumentOutOfRangeException(int interval)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MonthlyWeekdayRecurrenceStrategy(DayOfWeek.Monday, WeekOrdinal.First, interval);
        });

        Assert.AreEqual("intervalMonths", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an interval above 1 with no anchor date, which the loader rejects, throws <see cref="ArgumentException" />
    /// naming the <c>anchorDate</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalNeedsAnAnchorDate_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MonthlyWeekdayRecurrenceStrategy(DayOfWeek.Monday, WeekOrdinal.First, 2);
        });

        Assert.AreEqual("anchorDate", ex.ParamName);
    }
}

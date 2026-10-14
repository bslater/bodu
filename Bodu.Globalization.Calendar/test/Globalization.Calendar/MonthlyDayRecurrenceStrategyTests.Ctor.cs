// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthlyDayRecurrenceStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class MonthlyDayRecurrenceStrategyTests
{
    /// <summary>
    /// Verifies that a day of the month outside 1 to 31, which the loader rejects, throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>dayOfMonth</c> parameter.
    /// </summary>
    /// <param name="dayOfMonth">The day of the month.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(32)]
    public void Ctor_WhenDayOfMonthIsOutsideOneToThirtyOne_ShouldThrowArgumentOutOfRangeException(int dayOfMonth)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MonthlyDayRecurrenceStrategy(dayOfMonth);
        });

        Assert.AreEqual("dayOfMonth", ex.ParamName);
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
            _ = new MonthlyDayRecurrenceStrategy(1, interval);
        });

        Assert.AreEqual("intervalMonths", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an enumeration value its type does not define, which the loader rejects with <c>BODU-CAL-ENUM</c>,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the parameter, as the binary pack reader rejects it.
    /// </summary>
    /// <param name="parameter">The name of the parameter given the undefined value.</param>
    [TestMethod]
    [DataRow("invalidDayBehavior")]
    public void Ctor_WhenEnumValueIsUndefined_ShouldThrowArgumentOutOfRangeException(string parameter)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = parameter switch
            {
                "invalidDayBehavior" => new MonthlyDayRecurrenceStrategy(1, invalidDayBehavior: (InvalidDayOfMonthBehavior)99),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
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
            _ = new MonthlyDayRecurrenceStrategy(1, 2);
        });

        Assert.AreEqual("anchorDate", ex.ParamName);
    }
}

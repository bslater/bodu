// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeeklyRecurrenceStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WeeklyRecurrenceStrategyTests
{
    /// <summary>
    /// Verifies that an interval below 1, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" />
    /// naming the <c>intervalWeeks</c> parameter.
    /// </summary>
    /// <param name="interval">The interval.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenIntervalIsBelowOne_ShouldThrowArgumentOutOfRangeException(int interval)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new WeeklyRecurrenceStrategy([DayOfWeek.Monday], interval);
        });

        Assert.AreEqual("intervalWeeks", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty or repeated list of weekdays, which the loader rejects, throws <see cref="ArgumentException" />
    /// naming the <c>daysOfWeek</c> parameter.
    /// </summary>
    /// <param name="repeated"><see langword="true" /> to repeat a weekday; <see langword="false" /> to give none.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Ctor_WhenDaysAreEmptyOrRepeated_ShouldThrowArgumentException(bool repeated)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new WeeklyRecurrenceStrategy(repeated ? [DayOfWeek.Monday, DayOfWeek.Monday] : []);
        });

        Assert.AreEqual("daysOfWeek", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a weekday <see cref="DayOfWeek" /> does not define, which the loader rejects with <c>BODU-CAL-ENUM</c>,
    /// throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDayIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new WeeklyRecurrenceStrategy([(DayOfWeek)7]);
        });
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
            _ = new WeeklyRecurrenceStrategy([DayOfWeek.Monday], 2);
        });

        Assert.AreEqual("anchorDate", ex.ParamName);
    }
}

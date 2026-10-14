// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DailyIntervalRecurrenceStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DailyIntervalRecurrenceStrategyTests
{
    /// <summary>
    /// Verifies that an interval below 1, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" />
    /// naming the <c>intervalDays</c> parameter.
    /// </summary>
    /// <param name="interval">The interval.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenIntervalIsBelowOne_ShouldThrowArgumentOutOfRangeException(int interval)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DailyIntervalRecurrenceStrategy(new DateOnly(2024, 1, 1), interval);
        });

        Assert.AreEqual("intervalDays", ex.ParamName);
    }
}

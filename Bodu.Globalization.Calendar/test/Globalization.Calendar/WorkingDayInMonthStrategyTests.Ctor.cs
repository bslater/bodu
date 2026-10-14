// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayInMonthStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayInMonthStrategyTests
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
            _ = new WorkingDayInMonthStrategy(month, 1);
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an ordinal of zero, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" /> naming
    /// the <c>ordinal</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOrdinalIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new WorkingDayInMonthStrategy(5, 0);
        });

        Assert.AreEqual("ordinal", ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfYearStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DayOfYearStrategyTests
{
    /// <summary>
    /// Verifies that an ordinal of zero, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" /> naming
    /// the <c>ordinal</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOrdinalIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DayOfYearStrategy(0);
        });

        Assert.AreEqual("ordinal", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an ordinal outside -366 to 366, which the loader rejects, throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>ordinal</c> parameter, as the binary pack reader rejects it.
    /// </summary>
    /// <param name="ordinal">The ordinal.</param>
    [TestMethod]
    [DataRow(-367)]
    [DataRow(367)]
    public void Ctor_WhenOrdinalIsOutsideTheLoadersRange_ShouldThrowArgumentOutOfRangeException(int ordinal)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DayOfYearStrategy(ordinal);
        });

        Assert.AreEqual("ordinal", ex.ParamName);
    }
}

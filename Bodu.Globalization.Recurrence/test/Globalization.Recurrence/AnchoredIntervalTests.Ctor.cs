// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that a positive whole-second interval is stored unchanged.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsPositiveWholeSeconds_ShouldStoreInterval()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));

        Assert.AreEqual(TimeSpan.FromHours(4), interval.Interval);
    }

    /// <summary>
    /// Verifies that a zero interval throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new AnchoredInterval(TimeSpan.Zero);
        });

        Assert.AreEqual("interval", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative interval throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new AnchoredInterval(TimeSpan.FromHours(-4));
        });

        Assert.AreEqual("interval", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an interval carrying sub-second ticks throws <see cref="ArgumentException" />, because the
    /// canonical iCalendar duration text cannot represent it.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalHasSubSecondTicks_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AnchoredInterval(TimeSpan.FromMilliseconds(1500));
        });

        Assert.AreEqual("interval", ex.ParamName);
    }
}

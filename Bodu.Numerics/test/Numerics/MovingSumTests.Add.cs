// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSumTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class MovingSumTests
{
    /// <summary>
    /// Verifies that before the window fills the sum and mean describe every sample received so far.
    /// </summary>
    [TestMethod]
    public void Add_WhenWindowIsWarmingUp_ShouldDescribeAllSamplesSoFar()
    {
        var window = new MovingSum<int>(3);

        window.Add(4);
        Assert.AreEqual(4, window.Sum);
        Assert.AreEqual(4.0, window.Mean, 1e-12);
        Assert.IsFalse(window.IsFull);

        window.Add(6);
        Assert.AreEqual(10, window.Sum);
        Assert.AreEqual(5.0, window.Mean, 1e-12);

        window.Add(8);
        Assert.IsTrue(window.IsFull);
        Assert.AreEqual(18, window.Sum);
    }

    /// <summary>
    /// Verifies that once full, each new sample evicts the oldest and the sum tracks exactly the window contents
    /// through repeated wrap-around.
    /// </summary>
    [TestMethod]
    public void Add_WhenWindowWrapsRepeatedly_ShouldTrackExactWindowSum()
    {
        var window = new MovingSum<int>(2);

        window.Add(1);
        window.Add(2);
        window.Add(3);
        Assert.AreEqual(5, window.Sum);

        window.Add(4);
        Assert.AreEqual(7, window.Sum);

        window.Add(5);
        Assert.AreEqual(9, window.Sum);
        Assert.AreEqual(2, window.Count);
    }

    /// <summary>
    /// Verifies that a capacity-one window always reports the single most recent sample.
    /// </summary>
    [TestMethod]
    public void Add_WhenCapacityIsOne_ShouldTrackMostRecentSample()
    {
        var window = new MovingSum<double>(1);

        window.Add(3.5);
        Assert.AreEqual(3.5, window.Sum, 1e-12);

        window.Add(-1.5);
        Assert.AreEqual(-1.5, window.Sum, 1e-12);
        Assert.AreEqual(-1.5, window.Mean, 1e-12);
    }

    /// <summary>
    /// Verifies that decimal samples keep the window sum exact in <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingDecimals_ShouldKeepExactSum()
    {
        var window = new MovingSum<decimal>(2);
        window.Add(0.1m);
        window.Add(0.2m);
        window.Add(0.3m);

        Assert.AreEqual(0.5m, window.Sum);
    }

    /// <summary>
    /// Verifies that adding a NaN sample throws <see cref="ArgumentException" /> and leaves the window unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsNaN_ShouldThrowArgumentException()
    {
        var window = new MovingSum<double>(3);
        window.Add(1.0);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            window.Add(double.NaN);
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(1, window.Count);
    }

    /// <summary>
    /// Verifies that adding an infinite sample throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsPositiveInfinity_ShouldThrowArgumentException()
    {
        var window = new MovingSum<double>(3);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            window.Add(double.PositiveInfinity);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that reading <see cref="MovingSum{T}.Mean" /> on an empty window throws
    /// <see cref="InvalidOperationException" /> while <see cref="MovingSum{T}.Sum" /> reports zero.
    /// </summary>
    [TestMethod]
    public void Mean_WhenWindowIsEmpty_ShouldThrowInvalidOperationException()
    {
        var window = new MovingSum<double>(3);

        Assert.AreEqual(0.0, window.Sum);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = window.Mean;
        });
    }

    /// <summary>
    /// Verifies that <see cref="MovingSum{T}.Reset" /> empties the window and zeroes the sum.
    /// </summary>
    [TestMethod]
    public void Reset_WhenWindowHasSamples_ShouldReturnToEmptyState()
    {
        var window = new MovingSum<int>(3);
        window.Add(1);
        window.Add(2);
        window.Add(3);
        window.Add(4);

        window.Reset();

        Assert.IsTrue(window.IsEmpty);
        Assert.AreEqual(0, window.Sum);

        window.Add(5);
        Assert.AreEqual(5, window.Sum);
        Assert.AreEqual(1, window.Count);
    }
}

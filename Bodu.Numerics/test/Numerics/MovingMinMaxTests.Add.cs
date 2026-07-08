// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMaxTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class MovingMinMaxTests
{
    /// <summary>
    /// Verifies that before the window fills the extrema describe every sample received so far.
    /// </summary>
    [TestMethod]
    public void Add_WhenWindowIsWarmingUp_ShouldDescribeAllSamplesSoFar()
    {
        var window = new MovingMinMax<int>(4);

        window.Add(3);
        Assert.AreEqual(3, window.Minimum);
        Assert.AreEqual(3, window.Maximum);

        window.Add(-1);
        Assert.AreEqual(-1, window.Minimum);
        Assert.AreEqual(3, window.Maximum);

        window.Add(7);
        Assert.AreEqual(-1, window.Minimum);
        Assert.AreEqual(7, window.Maximum);
        Assert.IsFalse(window.IsFull);
    }

    /// <summary>
    /// Verifies that a strictly ascending run keeps the maximum current while the minimum slides with the window.
    /// </summary>
    [TestMethod]
    public void Add_WhenSamplesAscend_ShouldSlideMinimumWithWindow()
    {
        var window = new MovingMinMax<int>(3);
        foreach (var sample in new[] { 1, 2, 3, 4, 5 })
            window.Add(sample);

        Assert.AreEqual(3, window.Minimum);
        Assert.AreEqual(5, window.Maximum);
    }

    /// <summary>
    /// Verifies that a strictly descending run keeps the minimum current while the maximum slides with the window.
    /// </summary>
    [TestMethod]
    public void Add_WhenSamplesDescend_ShouldSlideMaximumWithWindow()
    {
        var window = new MovingMinMax<int>(3);
        foreach (var sample in new[] { 5, 4, 3, 2, 1 })
            window.Add(sample);

        Assert.AreEqual(1, window.Minimum);
        Assert.AreEqual(3, window.Maximum);
    }

    /// <summary>
    /// Verifies that duplicate samples are retained correctly — an equal older sample must not shadow a newer one as
    /// the window slides.
    /// </summary>
    [TestMethod]
    public void Add_WhenSamplesContainDuplicates_ShouldTrackExtremaThroughEviction()
    {
        var window = new MovingMinMax<int>(2);
        window.Add(2);
        window.Add(2);
        window.Add(5);

        Assert.AreEqual(2, window.Minimum);
        Assert.AreEqual(5, window.Maximum);

        window.Add(5);
        Assert.AreEqual(5, window.Minimum);
        Assert.AreEqual(5, window.Maximum);
    }

    /// <summary>
    /// Verifies that a capacity-one window always reports the single most recent sample as both extrema.
    /// </summary>
    [TestMethod]
    public void Add_WhenCapacityIsOne_ShouldTrackMostRecentSample()
    {
        var window = new MovingMinMax<double>(1);

        window.Add(3.5);
        Assert.AreEqual(3.5, window.Minimum);

        window.Add(-1.5);
        Assert.AreEqual(-1.5, window.Minimum);
        Assert.AreEqual(-1.5, window.Maximum);
    }

    /// <summary>
    /// Verifies that <see cref="long" /> samples track exact extrema through the deques.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingLongs_ShouldTrackExactExtrema()
    {
        var window = new MovingMinMax<long>(2);
        window.Add(-4_000_000_000L);
        window.Add(4_000_000_000L);
        window.Add(0L);

        Assert.AreEqual(0L, window.Minimum);
        Assert.AreEqual(4_000_000_000L, window.Maximum);
    }

    /// <summary>
    /// Verifies that <see cref="float" /> samples track exact single-precision extrema through the deques.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingFloats_ShouldTrackExactExtrema()
    {
        var window = new MovingMinMax<float>(2);
        window.Add(1.25f);
        window.Add(-2.75f);
        window.Add(0.5f);

        Assert.AreEqual(-2.75f, window.Minimum);
        Assert.AreEqual(0.5f, window.Maximum);
    }

    /// <summary>
    /// Verifies that adding a NaN sample throws <see cref="ArgumentException" /> and leaves the window unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsNaN_ShouldThrowArgumentException()
    {
        var window = new MovingMinMax<double>(3);
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
    public void Add_WhenSampleIsNegativeInfinity_ShouldThrowArgumentException()
    {
        var window = new MovingMinMax<double>(3);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            window.Add(double.NegativeInfinity);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that reading the extrema of an empty window throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Minimum_WhenWindowIsEmpty_ShouldThrowInvalidOperationException()
    {
        var window = new MovingMinMax<int>(3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = window.Minimum;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = window.Maximum;
        });
    }

    /// <summary>
    /// Verifies that <see cref="MovingMinMax{T}.Reset" /> empties the window and it accepts new samples afterwards.
    /// </summary>
    [TestMethod]
    public void Reset_WhenWindowHasSamples_ShouldReturnToEmptyState()
    {
        var window = new MovingMinMax<int>(3);
        window.Add(1);
        window.Add(9);

        window.Reset();

        Assert.IsTrue(window.IsEmpty);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = window.Minimum;
        });

        window.Add(4);
        Assert.AreEqual(4, window.Minimum);
        Assert.AreEqual(4, window.Maximum);
    }
}

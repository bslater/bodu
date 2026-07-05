// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Clear" /> resets a populated sketch so that
    /// <see cref="HyperLogLog{T}.EstimateCardinality" /> returns exactly zero again.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSketchIsPopulated_ShouldResetEstimateToZero()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        for (var i = 0; i < 5000; i++)
            sketch.Add(i);
        Assert.IsTrue(sketch.EstimateCardinality() > 0.0);

        sketch.Clear();

        Assert.AreEqual(0.0, sketch.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Clear" /> on an empty sketch is a no-op that leaves the estimate at
    /// zero and the configuration unchanged.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSketchIsEmpty_ShouldRemainEmpty()
    {
        var sketch = new HyperLogLog<int>(precision: 10);

        sketch.Clear();

        Assert.AreEqual(0.0, sketch.EstimateCardinality());
        Assert.AreEqual(10, sketch.Precision);
        Assert.AreEqual(1024, sketch.RegisterCount);
    }

    /// <summary>
    /// Verifies that a cleared sketch is fully reusable: repopulating it with the same elements reproduces the same
    /// estimate as the original deterministic fill.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSketchIsRepopulated_ShouldReproduceOriginalEstimate()
    {
        var sketch = new HyperLogLog<int>(precision: 12);
        for (var i = 0; i < 2000; i++)
            sketch.Add(i);
        var original = sketch.EstimateCardinality();

        sketch.Clear();
        for (var i = 0; i < 2000; i++)
            sketch.Add(i);

        Assert.AreEqual(original, sketch.EstimateCardinality());
    }
}

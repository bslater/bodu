// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Clear" /> removes all previously accumulated counts, so every prior
    /// item subsequently reports an estimate of zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSketchHasCounts_ShouldResetAllEstimatesToZero()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        for (var i = 0; i < 500; i++)
            sketch.Add(i, i + 1);

        sketch.Clear();

        for (var i = 0; i < 500; i++)
            Assert.AreEqual(0L, sketch.EstimateCount(i), $"Item {i} still reported a count after Clear.");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Clear" /> resets <see cref="CountMinSketch{T}.TotalCount" /> to
    /// zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSketchHasCounts_ShouldResetTotalCountToZero()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        sketch.Add(1, 100);
        Assert.AreEqual(100L, sketch.TotalCount);

        sketch.Clear();

        Assert.AreEqual(0L, sketch.TotalCount);
    }

    /// <summary>
    /// Verifies that calling <see cref="CountMinSketch{T}.Clear" /> on an already-empty sketch leaves it usable:
    /// counts added afterwards are tracked normally.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledThenCountsReAdded_ShouldTrackNewCounts()
    {
        var sketch = new CountMinSketch<int>(0.001, 0.001);
        sketch.Clear();

        sketch.Add(7, 3);

        Assert.AreEqual(3L, sketch.EstimateCount(7));
        Assert.AreEqual(3L, sketch.TotalCount);
    }
}

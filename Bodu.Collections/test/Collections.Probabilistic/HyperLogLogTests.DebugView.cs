// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that <see cref="HyperLogLogDebugView{T}" /> surfaces the estimator's geometry and its current
    /// cardinality estimate.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenEstimatorPopulated_ShouldSurfaceGeometryAndEstimate()
    {
        var sketch = new HyperLogLog<string>(12);
        for (var i = 0; i < 100; i++)
        {
            sketch.Add($"item-{i}");
        }

        var view = new HyperLogLogDebugView<string>(sketch);

        Assert.AreEqual(sketch.Precision, view.Precision);
        Assert.AreEqual(sketch.RegisterCount, view.RegisterCount);
        Assert.AreEqual(sketch.StandardError, view.StandardError);
        Assert.AreEqual(sketch.EstimateCardinality(), view.EstimatedCardinality);
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLogDebugView{T}.EstimatedCardinality" /> is zero for an unpopulated
    /// estimator.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenEstimatorEmpty_ShouldReportZeroEstimatedCardinality()
    {
        var sketch = new HyperLogLog<string>(12);

        var view = new HyperLogLogDebugView<string>(sketch);

        Assert.AreEqual(0d, view.EstimatedCardinality);
    }

    /// <summary>
    /// Verifies that constructing <see cref="HyperLogLogDebugView{T}" /> with a <see langword="null" />
    /// estimator throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenEstimatorIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new HyperLogLogDebugView<string>(null!);
        });

        Assert.AreEqual("sketch", ex.ParamName);
    }
}

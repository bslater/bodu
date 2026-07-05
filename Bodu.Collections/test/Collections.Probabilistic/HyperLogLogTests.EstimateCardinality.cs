// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.EstimateCardinality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that an empty sketch estimates exactly zero: with every register zero the raw estimate falls in the
    /// small range, and linear counting evaluates <c>m·ln(m/V)</c> with <c>V = m</c>, which is exactly 0.
    /// </summary>
    [TestMethod]
    [DataRow(4)]
    [DataRow(10)]
    [DataRow(14)]
    public void EstimateCardinality_WhenSketchIsEmpty_ShouldReturnExactlyZero(int precision)
    {
        var sketch = new HyperLogLog<int>(precision);

        Assert.AreEqual(0.0, sketch.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that the small-range linear-counting path estimates a lightly filled sketch accurately: 100 distinct
    /// items at precision 14 (16,384 registers, most still zero) land within 10% of the true count.
    /// </summary>
    [TestMethod]
    public void EstimateCardinality_WhenSketchIsLightlyFilled_ShouldUseAccurateLinearCounting()
    {
        var sketch = new HyperLogLog<int>(precision: 14);

        for (var i = 0; i < 100; i++)
            sketch.Add(i);

        var estimate = sketch.EstimateCardinality();

        Assert.IsTrue(Math.Abs(estimate - 100.0) / 100.0 < 0.10, $"Linear-counting estimate {estimate} deviated more than 10% from 100.");
    }

    /// <summary>
    /// Verifies loose growth behaviour on a coarse sketch: at precision 4 (16 registers) the estimate still increases
    /// through the harmonic-mean path as the distinct count grows by orders of magnitude.
    /// </summary>
    [TestMethod]
    public void EstimateCardinality_WhenDistinctCountGrowsOnCoarseSketch_ShouldGrowEstimate()
    {
        var sketch = new HyperLogLog<int>(precision: 4);
        var previous = 0.0;

        var added = 0;
        foreach (var target in new[] { 100, 10_000, 1_000_000 })
        {
            while (added < target)
                sketch.Add(added++);

            var estimate = sketch.EstimateCardinality();

            Assert.IsTrue(estimate > previous, $"Estimate {estimate} after {target} distinct items did not exceed the previous estimate {previous}.");
            previous = estimate;
        }
    }

    /// <summary>
    /// Verifies that the precision-14 estimator holds a 3-sigma relative error bound over a sweep of distinct counts:
    /// for n distinct sequential integers, |estimate − n|/n stays below 3 · <see cref="HyperLogLog{T}.StandardError" />
    /// (~2.4% at precision 14). Three standard errors is used because roughly 99% of estimates fall within that band,
    /// making the deterministic seeded sweep a stable regression pin rather than a flaky statistical coin flip.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(10_000)]
    [DataRow(100_000)]
    public void EstimateCardinality_WhenSweptWithDistinctSequentialInts_ShouldStayWithinThreeSigma(int distinctCount)
    {
        var sketch = new HyperLogLog<int>(precision: 14);

        for (var i = 0; i < distinctCount; i++)
            sketch.Add(i);

        var estimate = sketch.EstimateCardinality();
        var relativeError = Math.Abs(estimate - distinctCount) / distinctCount;

        Assert.IsTrue(relativeError < 3 * sketch.StandardError, $"Estimate {estimate} for {distinctCount} distinct items has relative error {relativeError:P2}, exceeding the 3-sigma bound {3 * sketch.StandardError:P2}.");
    }
}

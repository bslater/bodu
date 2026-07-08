// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Verifies that while fewer than five samples are held the estimate is the exact linearly interpolated
    /// empirical quantile.
    /// </summary>
    [TestMethod]
    public void Add_WhenFewerThanFiveSamples_ShouldReturnEmpiricalInterpolatedQuantile()
    {
        var median = RunningQuantile<double>.CreateMedian();

        median.Add(3.0);
        Assert.AreEqual(3.0, median.Value, 1e-12);

        median.Add(1.0);
        Assert.AreEqual(2.0, median.Value, 1e-12);

        median.Add(5.0);
        Assert.AreEqual(3.0, median.Value, 1e-12);

        median.Add(7.0);
        Assert.AreEqual(4.0, median.Value, 1e-12);
    }

    /// <summary>
    /// Verifies the warm-up interpolation for a non-median probability against the hand-computed empirical
    /// quantile.
    /// </summary>
    [TestMethod]
    public void Add_WhenFewerThanFiveSamples_ForNonMedianProbability_ShouldInterpolateEmpirically()
    {
        var quartile = new RunningQuantile<double>(0.25);
        foreach (var sample in new[] { 4.0, 1.0, 3.0, 2.0 })
            quartile.Add(sample);

        // Sorted {1, 2, 3, 4}: rank h = 3 × 0.25 = 0.75 → 1 + 0.75 × (2 − 1) = 1.75.
        Assert.AreEqual(1.75, quartile.Value, 1e-12);
    }

    /// <summary>
    /// Verifies that integer samples accumulate through the same widened estimator path.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingInts_ShouldEstimateMedian()
    {
        var median = RunningQuantile<int>.CreateMedian();
        foreach (var sample in new[] { 9, 1, 5, 3, 7 })
            median.Add(sample);

        Assert.AreEqual(5.0, median.Value, 1e-12);
    }

    /// <summary>
    /// Verifies that adding a NaN sample throws <see cref="ArgumentException" /> and leaves the estimator
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsNaN_ShouldThrowArgumentException()
    {
        var median = RunningQuantile<double>.CreateMedian();
        median.Add(1.0);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            median.Add(double.NaN);
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(1L, median.Count);
    }

    /// <summary>
    /// Verifies that adding an infinite sample throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsNegativeInfinity_ShouldThrowArgumentException()
    {
        var median = RunningQuantile<double>.CreateMedian();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            median.Add(double.NegativeInfinity);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that reading <see cref="RunningQuantile{T}.Value" /> on an empty estimator throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Value_WhenEstimatorIsEmpty_ShouldThrowInvalidOperationException()
    {
        var median = RunningQuantile<double>.CreateMedian();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = median.Value;
        });
    }

    /// <summary>
    /// Verifies that <see cref="RunningQuantile{T}.Reset" /> discards the samples but preserves the target
    /// probability.
    /// </summary>
    [TestMethod]
    public void Reset_WhenEstimatorHasSamples_ShouldClearSamplesAndKeepProbability()
    {
        var p90 = new RunningQuantile<double>(0.9);
        foreach (var observation in PaperObservations)
            p90.Add(observation);

        p90.Reset();

        Assert.AreEqual(0.9, p90.Probability, 1e-15);
        Assert.IsTrue(p90.IsEmpty);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = p90.Value;
        });
    }

    /// <summary>
    /// Verifies that copying the estimator snapshots its state — the copy and the original accumulate independently
    /// afterwards.
    /// </summary>
    [TestMethod]
    public void Add_WhenEstimatorIsCopied_ShouldAccumulateIndependently()
    {
        var original = RunningQuantile<double>.CreateMedian();
        foreach (var observation in PaperObservations)
            original.Add(observation);

        var snapshot = original;
        original.Add(1000.0);

        Assert.AreEqual(21L, original.Count);
        Assert.AreEqual(20L, snapshot.Count);
        Assert.AreEqual(4.44063, snapshot.Value, 1e-4);
    }
}

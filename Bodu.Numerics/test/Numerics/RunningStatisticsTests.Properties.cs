// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningStatisticsTests
{
    /// <summary>
    /// Verifies that every result property except <see cref="RunningStatistics{T}.Count" /> and
    /// <see cref="RunningStatistics{T}.IsEmpty" /> throws <see cref="InvalidOperationException" /> on an empty
    /// accumulator.
    /// </summary>
    [TestMethod]
    public void Properties_WhenAccumulatorIsEmpty_ShouldThrowInvalidOperationException()
    {
        var stats = new RunningStatistics<double>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.Mean;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.Minimum;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.Maximum;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.PopulationVariance;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.PopulationStandardDeviation;
        });
    }

    /// <summary>
    /// Verifies that the default value is the valid empty accumulator.
    /// </summary>
    [TestMethod]
    public void Properties_WhenDefaultValue_ShouldBeEmptyAndUsable()
    {
        var stats = default(RunningStatistics<int>);

        Assert.IsTrue(stats.IsEmpty);
        Assert.AreEqual(0L, stats.Count);

        stats.Add(7);

        Assert.IsFalse(stats.IsEmpty);
        Assert.AreEqual(7, stats.Minimum);
    }

    /// <summary>
    /// Verifies that a single sample yields a population variance of zero while the sample variance throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void SampleVariance_WhenSingleSample_ShouldThrowInvalidOperationException()
    {
        var stats = Accumulate(5.0);

        Assert.AreEqual(0.0, stats.PopulationVariance, 1e-12);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.SampleVariance;
        });
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.SampleStandardDeviation;
        });
    }

    /// <summary>
    /// Verifies that <see cref="RunningStatistics{T}.Reset" /> returns the accumulator to the empty state.
    /// </summary>
    [TestMethod]
    public void Reset_WhenAccumulatorHasSamples_ShouldReturnToEmptyState()
    {
        var stats = Accumulate(1.0, 2.0, 3.0);

        stats.Reset();

        Assert.IsTrue(stats.IsEmpty);
        Assert.AreEqual(0L, stats.Count);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = stats.Mean;
        });
    }

    /// <summary>
    /// Verifies that copying the accumulator snapshots its state — the copy and the original accumulate
    /// independently afterwards.
    /// </summary>
    [TestMethod]
    public void Properties_WhenCopied_ShouldSnapshotIndependently()
    {
        var original = Accumulate(1.0, 2.0);
        var snapshot = original;

        original.Add(30.0);

        Assert.AreEqual(3L, original.Count);
        Assert.AreEqual(2L, snapshot.Count);
        Assert.AreEqual(2.0, snapshot.Maximum);
    }

    /// <summary>
    /// Verifies that <see cref="RunningStatistics{T}.ToString" /> reports the empty state and a filled summary in
    /// culture-invariant form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEmptyAndFilled_ShouldReportInvariantSummary()
    {
        var stats = new RunningStatistics<int>();

        Assert.AreEqual("Count = 0", stats.ToString());

        stats.Add(2);
        stats.Add(4);

        Assert.AreEqual("Count = 2, Mean = 3, Min = 2, Max = 4", stats.ToString());
    }
}

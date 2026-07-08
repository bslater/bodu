// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.Combine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningStatisticsTests
{
    /// <summary>
    /// Verifies that combining with an empty accumulator returns the other operand unchanged, on both sides.
    /// </summary>
    [TestMethod]
    public void Combine_WhenOneOperandIsEmpty_ShouldReturnOtherUnchanged()
    {
        var empty = new RunningStatistics<double>();
        var filled = Accumulate(1.0, 2.0, 3.0);

        var leftIdentity = RunningStatistics<double>.Combine(empty, filled);
        var rightIdentity = RunningStatistics<double>.Combine(filled, empty);

        Assert.AreEqual(3L, leftIdentity.Count);
        Assert.AreEqual(filled.Mean, leftIdentity.Mean, 1e-15);
        Assert.AreEqual(3L, rightIdentity.Count);
        Assert.AreEqual(filled.Mean, rightIdentity.Mean, 1e-15);
    }

    /// <summary>
    /// Verifies that combining two empty accumulators yields the empty accumulator.
    /// </summary>
    [TestMethod]
    public void Combine_WhenBothOperandsAreEmpty_ShouldReturnEmpty()
    {
        var combined = RunningStatistics<int>.Combine(default, default);

        Assert.IsTrue(combined.IsEmpty);
    }

    /// <summary>
    /// Verifies that combining two partitions reproduces the moments of accumulating the concatenated stream.
    /// </summary>
    [TestMethod]
    public void Combine_WhenMergingTwoPartitions_ShouldMatchSequentialAccumulation()
    {
        var left = Accumulate(1.0, 2.0, 3.0);
        var right = Accumulate(4.0, 5.0, 6.0, 7.0);

        var combined = RunningStatistics<double>.Combine(left, right);
        var sequential = Accumulate(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0);

        Assert.AreEqual(7L, combined.Count);
        Assert.AreEqual(1.0, combined.Minimum);
        Assert.AreEqual(7.0, combined.Maximum);
        Assert.AreEqual(sequential.Mean, combined.Mean, 1e-12);
        Assert.AreEqual(sequential.PopulationVariance, combined.PopulationVariance, 1e-12);
        Assert.AreEqual(4.0, combined.PopulationVariance, 1e-12);
    }
}

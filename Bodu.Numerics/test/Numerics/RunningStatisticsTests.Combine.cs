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

    /// <summary>
    /// Verifies that heavily imbalanced partitions (one sample versus many) merge to the sequential result within
    /// floating-point tolerance.
    /// </summary>
    [TestMethod]
    public void Combine_WhenPartitionsAreImbalanced_ShouldMatchSequentialWithinTolerance()
    {
        var random = new Random(31);
        var samples = Enumerable.Range(0, 1000).Select(_ => (random.NextDouble() * 2000.0) - 1000.0).ToArray();

        var sequential = new RunningStatistics<double>();
        var single = new RunningStatistics<double>();
        var rest = new RunningStatistics<double>();

        foreach (var sample in samples)
            sequential.Add(sample);

        single.Add(samples[0]);
        foreach (var sample in samples.Skip(1))
            rest.Add(sample);

        var combined = RunningStatistics<double>.Combine(single, rest);

        Assert.AreEqual(sequential.Count, combined.Count);
        Assert.AreEqual(sequential.Mean, combined.Mean, Math.Abs(sequential.Mean) * 1e-9 + 1e-9);
        Assert.AreEqual(sequential.PopulationVariance, combined.PopulationVariance, sequential.PopulationVariance * 1e-9);
    }

    /// <summary>
    /// Verifies that different merge trees over the same partitions agree within floating-point tolerance —
    /// floating-point addition is not associative, so exact bitwise equality is deliberately not asserted.
    /// </summary>
    [TestMethod]
    public void Combine_WhenMergeTreesDiffer_ShouldAgreeWithinTolerance()
    {
        var random = new Random(67);
        var partA = new RunningStatistics<double>();
        var partB = new RunningStatistics<double>();
        var partC = new RunningStatistics<double>();

        for (var i = 0; i < 10; i++)
            partA.Add((random.NextDouble() * 20.0) - 10.0);
        for (var i = 0; i < 500; i++)
            partB.Add((random.NextDouble() * 20.0) - 10.0);
        for (var i = 0; i < 37; i++)
            partC.Add((random.NextDouble() * 20.0) - 10.0);

        var leftTree = RunningStatistics<double>.Combine(RunningStatistics<double>.Combine(partA, partB), partC);
        var rightTree = RunningStatistics<double>.Combine(partA, RunningStatistics<double>.Combine(partB, partC));

        Assert.AreEqual(leftTree.Count, rightTree.Count);
        Assert.AreEqual(leftTree.Minimum, rightTree.Minimum);
        Assert.AreEqual(leftTree.Maximum, rightTree.Maximum);
        Assert.AreEqual(leftTree.Mean, rightTree.Mean, Math.Abs(leftTree.Mean) * 1e-9 + 1e-12);
        Assert.AreEqual(leftTree.PopulationVariance, rightTree.PopulationVariance, leftTree.PopulationVariance * 1e-9);
    }
}

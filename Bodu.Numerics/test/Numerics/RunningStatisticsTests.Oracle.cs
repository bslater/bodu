// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

public partial class RunningStatisticsTests
{
    /// <summary>
    /// Verifies that over a seeded random stream the accumulator matches a naive two-pass recomputation of the mean,
    /// variance, and extrema at every prefix length.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenSweepingSeededStreamPrefixes_ShouldMatchTwoPassOracle()
    {
        var random = new Random(1234);
        var samples = new List<double>();
        var stats = new RunningStatistics<double>();

        for (var n = 1; n <= 1000; n++)
        {
            var sample = (random.NextDouble() * 2000.0) - 1000.0;
            samples.Add(sample);
            stats.Add(sample);

            var mean = samples.Average();
            var m2 = samples.Sum(s => (s - mean) * (s - mean));

            Assert.AreEqual(n, stats.Count, $"Count diverged at n = {n}.");
            Assert.AreEqual(samples.Min(), stats.Minimum, $"Minimum diverged at n = {n}.");
            Assert.AreEqual(samples.Max(), stats.Maximum, $"Maximum diverged at n = {n}.");
            Assert.AreEqual(mean, stats.Mean, Math.Abs(mean) * 1e-9 + 1e-9, $"Mean diverged at n = {n}.");
            Assert.AreEqual(m2 / n, stats.PopulationVariance, (m2 / n * 1e-9) + 1e-9, $"PopulationVariance diverged at n = {n}.");
        }
    }

    /// <summary>
    /// Verifies the merge law: for every split point of a seeded stream, combining the two partition accumulators
    /// equals accumulating the concatenated stream.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Combine_WhenSweepingEverySplitPoint_ShouldEqualSequentialAccumulation()
    {
        var random = new Random(5678);
        var samples = Enumerable.Range(0, 200).Select(_ => (random.NextDouble() * 200.0) - 100.0).ToArray();

        var sequential = new RunningStatistics<double>();
        foreach (var sample in samples)
            sequential.Add(sample);

        for (var split = 0; split <= samples.Length; split++)
        {
            var left = new RunningStatistics<double>();
            var right = new RunningStatistics<double>();

            for (var i = 0; i < split; i++)
                left.Add(samples[i]);
            for (var i = split; i < samples.Length; i++)
                right.Add(samples[i]);

            var combined = RunningStatistics<double>.Combine(left, right);

            Assert.AreEqual(sequential.Count, combined.Count, $"Count diverged at split = {split}.");
            Assert.AreEqual(sequential.Minimum, combined.Minimum, $"Minimum diverged at split = {split}.");
            Assert.AreEqual(sequential.Maximum, combined.Maximum, $"Maximum diverged at split = {split}.");
            Assert.AreEqual(sequential.Mean, combined.Mean, Math.Abs(sequential.Mean) * 1e-9 + 1e-9, $"Mean diverged at split = {split}.");
            Assert.AreEqual(
                sequential.PopulationVariance,
                combined.PopulationVariance,
                sequential.PopulationVariance * 1e-9,
                $"PopulationVariance diverged at split = {split}.");
        }
    }
}

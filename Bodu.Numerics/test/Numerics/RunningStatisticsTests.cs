// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for <see cref="RunningStatistics{T}" />.
/// </summary>
[TestClass]
public partial class RunningStatisticsTests
{
    /// <summary>
    /// Builds an accumulator over the given samples.
    /// </summary>
    /// <param name="samples">The samples to accumulate, in order.</param>
    /// <returns>The filled accumulator.</returns>
    private static RunningStatistics<double> Accumulate(params double[] samples)
    {
        var stats = new RunningStatistics<double>();
        foreach (var sample in samples)
            stats.Add(sample);

        return stats;
    }

    /// <summary>
    /// Verifies that a representative sample stream produces the expected count, mean, extrema, and standard
    /// deviation (smoke test).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void RunningStatistics_WhenAccumulatingSamples_ShouldReportExpectedMoments()
    {
        var stats = Accumulate(2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0);

        Assert.AreEqual(8L, stats.Count);
        Assert.AreEqual(5.0, stats.Mean, 1e-12);
        Assert.AreEqual(2.0, stats.Minimum);
        Assert.AreEqual(9.0, stats.Maximum);
        Assert.AreEqual(2.0, stats.PopulationStandardDeviation, 1e-12);
    }
}

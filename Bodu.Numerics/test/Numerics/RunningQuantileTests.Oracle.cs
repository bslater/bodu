// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Computes the exact linearly interpolated quantile of a sample set, the reference the P² estimate is compared
    /// against.
    /// </summary>
    /// <param name="samples">The samples to rank.</param>
    /// <param name="probability">The quantile to compute.</param>
    /// <returns>The exact interpolated quantile.</returns>
    private static double ExactQuantile(IReadOnlyList<double> samples, double probability)
    {
        var sorted = samples.OrderBy(s => s).ToArray();
        var rank = (sorted.Length - 1) * probability;
        var index = (int)rank;

        if (index + 1 >= sorted.Length)
            return sorted[^1];

        return sorted[index] + ((rank - index) * (sorted[index + 1] - sorted[index]));
    }

    /// <summary>
    /// Verifies that over a long seeded uniform stream the P² estimate lands within two percent of the sample range
    /// of the exact quantile, for a spread of probabilities.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Value_WhenEstimatingUniformStreamQuantiles_ShouldStayWithinTolerance()
    {
        foreach (var probability in new[] { 0.1, 0.5, 0.9, 0.95 })
        {
            var random = new Random(42);
            var estimator = new RunningQuantile<double>(probability);
            var samples = new List<double>(10_000);

            for (var i = 0; i < 10_000; i++)
            {
                var sample = random.NextDouble();
                samples.Add(sample);
                estimator.Add(sample);
            }

            var exact = ExactQuantile(samples, probability);
            var range = samples.Max() - samples.Min();

            Assert.AreEqual(
                exact,
                estimator.Value,
                0.02 * range,
                $"P² estimate diverged from the exact quantile for p = {probability}.");
        }
    }

    /// <summary>
    /// Verifies that over a heavy-tailed (exponential-like) seeded stream the P² median stays within two percent of
    /// the sample range of the exact median.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Value_WhenEstimatingExponentialStreamMedian_ShouldStayWithinTolerance()
    {
        var random = new Random(97);
        var estimator = RunningQuantile<double>.CreateMedian();
        var samples = new List<double>(10_000);

        for (var i = 0; i < 10_000; i++)
        {
            var sample = -Math.Log(1.0 - random.NextDouble());
            samples.Add(sample);
            estimator.Add(sample);
        }

        var exact = ExactQuantile(samples, 0.5);
        var range = samples.Max() - samples.Min();

        Assert.AreEqual(exact, estimator.Value, 0.02 * range, "P² median diverged from the exact median.");
    }

    /// <summary>
    /// Verifies that for every prefix shorter than five samples the estimate equals the exact empirical
    /// interpolated quantile, across a spread of probabilities.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Value_WhenSweepingWarmUpPrefixes_ShouldEqualExactEmpiricalQuantile()
    {
        var random = new Random(7);

        foreach (var probability in new[] { 0.1, 0.25, 0.5, 0.75, 0.9 })
        {
            for (var trial = 0; trial < 50; trial++)
            {
                var estimator = new RunningQuantile<double>(probability);
                var samples = new List<double>();

                for (var length = 1; length <= 4; length++)
                {
                    var sample = (random.NextDouble() * 200.0) - 100.0;
                    samples.Add(sample);
                    estimator.Add(sample);

                    Assert.AreEqual(
                        ExactQuantile(samples, probability),
                        estimator.Value,
                        1e-12,
                        $"Warm-up estimate diverged for p = {probability}, prefix length {length}, trial {trial}.");
                }
            }
        }
    }
}

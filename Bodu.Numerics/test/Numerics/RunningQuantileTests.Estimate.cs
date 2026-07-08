// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.Estimate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Verifies that a strictly ascending stream — repeated high-side marker movement — keeps the median estimate
    /// near the exact median.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenSamplesAscendStrictly_ShouldStayNearExactMedian()
    {
        var median = RunningQuantile<double>.CreateMedian();
        var samples = Enumerable.Range(1, 100).Select(i => (double)i).ToList();

        foreach (var sample in samples)
            median.Add(sample);

        var exact = ExactQuantile(samples, 0.5);
        Assert.AreEqual(exact, median.Estimate, 0.05 * 99.0, "Median estimate diverged on the ascending stream.");
    }

    /// <summary>
    /// Verifies that a strictly descending stream — repeated low-side marker movement — keeps the median estimate
    /// near the exact median.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenSamplesDescendStrictly_ShouldStayNearExactMedian()
    {
        var median = RunningQuantile<double>.CreateMedian();
        var samples = Enumerable.Range(1, 100).Select(i => (double)(101 - i)).ToList();

        foreach (var sample in samples)
            median.Add(sample);

        var exact = ExactQuantile(samples, 0.5);
        Assert.AreEqual(exact, median.Estimate, 0.05 * 99.0, "Median estimate diverged on the descending stream.");
    }

    /// <summary>
    /// Verifies that a duplicate-heavy stream — equal marker heights — keeps the marker ordering intact: the median
    /// estimate stays between the mass points that straddle the exact median. P²'s parabolic interpolation
    /// deliberately smooths between discrete mass points, so exact-median equality is not the contract here.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenSamplesAreDuplicateHeavy_ShouldStayBetweenStraddlingMassPoints()
    {
        var median = RunningQuantile<double>.CreateMedian();
        var pattern = new[] { 1.0, 1.0, 1.0, 5.0, 5.0, 9.0 };

        for (var i = 0; i < 200; i++)
            median.Add(pattern[i % pattern.Length]);

        // Half the mass sits at 1 and the next mass point is 5; the P² median marker interpolates between them.
        Assert.IsTrue(
            median.Estimate >= 1.0 && median.Estimate <= 5.0,
            $"Median estimate {median.Estimate} escaped the mass points straddling the exact median.");
    }

    /// <summary>
    /// Verifies that a two-point distribution — the common telemetry fast-path/slow-path shape — keeps the median
    /// estimate between the two mass points and near the interpolated exact median.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenDistributionIsTwoPoint_ShouldStayBetweenTheMassPoints()
    {
        var median = RunningQuantile<double>.CreateMedian();
        var samples = new List<double>();

        for (var i = 0; i < 500; i++)
        {
            var sample = i % 2 == 0 ? 0.0 : 100.0;
            samples.Add(sample);
            median.Add(sample);
        }

        Assert.IsTrue(median.Estimate >= 0.0 && median.Estimate <= 100.0, "Estimate escaped the sample range.");
        Assert.AreEqual(ExactQuantile(samples, 0.5), median.Estimate, 50.0, "Median estimate diverged on the two-point stream.");
    }

    /// <summary>
    /// Verifies the near-boundary probabilities 0.01 and 0.99 against the exact quantile over a long seeded uniform
    /// stream.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenProbabilityIsNearBoundary_ShouldStayWithinTolerance()
    {
        foreach (var probability in new[] { 0.01, 0.99 })
        {
            var random = new Random(4242);
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

            Assert.AreEqual(exact, estimator.Estimate, 0.02 * range, $"Estimate diverged for boundary p = {probability}.");
        }
    }

    /// <summary>
    /// Verifies the affine-equivariance law: estimating over the transformed stream a·x + b (a &gt; 0) equals
    /// transforming the estimate, up to floating-point rounding — the P² update is linear in the marker heights.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenStreamIsAffineTransformed_ShouldTransformTheEstimate()
    {
        const double A = 3.5;
        const double B = -10.0;
        var random = new Random(9);
        var original = new RunningQuantile<double>(0.75);
        var transformed = new RunningQuantile<double>(0.75);

        for (var i = 0; i < 2_000; i++)
        {
            var sample = (random.NextDouble() * 40.0) - 20.0;
            original.Add(sample);
            transformed.Add((A * sample) + B);
        }

        var expected = (A * original.Estimate) + B;
        Assert.AreEqual(expected, transformed.Estimate, Math.Abs(expected) * 1e-9 + 1e-9);
    }

    /// <summary>
    /// Verifies the warm-up boundary sample counts: zero samples throw, one through four return the exact empirical
    /// quantile, and the fifth switches to the P² markers whose median equals the middle sorted sample.
    /// </summary>
    [TestMethod]
    public void Estimate_WhenSweepingWarmUpBoundary_ShouldMatchDocumentedBehaviour()
    {
        var median = RunningQuantile<double>.CreateMedian();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = median.Estimate;
        });

        var samples = new List<double>();
        foreach (var sample in new[] { 9.0, 1.0, 7.0, 3.0 })
        {
            samples.Add(sample);
            median.Add(sample);
            Assert.AreEqual(ExactQuantile(samples, 0.5), median.Estimate, 1e-12, $"Warm-up estimate diverged at n = {samples.Count}.");
        }

        // The fifth sample initializes the markers from the five sorted samples; the median marker is the middle one.
        median.Add(5.0);
        Assert.AreEqual(5.0, median.Estimate, 1e-12);
    }
}

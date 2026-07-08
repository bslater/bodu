// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class RunningStatisticsTests
{
    /// <summary>
    /// Verifies that accumulating a hand-computed double stream produces the exact textbook mean and variances.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingDoubles_ShouldMatchHandComputedMoments()
    {
        var stats = Accumulate(2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0);

        Assert.AreEqual(5.0, stats.Mean, 1e-12);
        Assert.AreEqual(4.0, stats.PopulationVariance, 1e-12);
        Assert.AreEqual(32.0 / 7.0, stats.SampleVariance, 1e-12);
        Assert.AreEqual(Math.Sqrt(32.0 / 7.0), stats.SampleStandardDeviation, 1e-12);
    }

    /// <summary>
    /// Verifies that integer samples accumulate with exact extrema in <see cref="int" /> and the expected
    /// floating-point moments.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingInts_ShouldTrackExactExtremaAndMoments()
    {
        var stats = new RunningStatistics<int>();
        foreach (var sample in new[] { -3, 0, 3, 6 })
            stats.Add(sample);

        Assert.AreEqual(4L, stats.Count);
        Assert.AreEqual(-3, stats.Minimum);
        Assert.AreEqual(6, stats.Maximum);
        Assert.AreEqual(1.5, stats.Mean, 1e-12);
        Assert.AreEqual(11.25, stats.PopulationVariance, 1e-12);
    }

    /// <summary>
    /// Verifies that <see cref="long" /> samples accumulate with exact extrema and the expected moments.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingLongs_ShouldTrackExactExtremaAndMoments()
    {
        var stats = new RunningStatistics<long>();
        foreach (var sample in new[] { -4_000_000_000L, 0L, 4_000_000_000L })
            stats.Add(sample);

        Assert.AreEqual(-4_000_000_000L, stats.Minimum);
        Assert.AreEqual(4_000_000_000L, stats.Maximum);
        Assert.AreEqual(0.0, stats.Mean, 1e-6);
    }

    /// <summary>
    /// Verifies that <see cref="float" /> samples accumulate with exact single-precision extrema and
    /// double-precision moments.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingFloats_ShouldTrackExactExtremaAndMoments()
    {
        var stats = new RunningStatistics<float>();
        foreach (var sample in new[] { 1.5f, 2.5f, 3.5f })
            stats.Add(sample);

        Assert.AreEqual(1.5f, stats.Minimum);
        Assert.AreEqual(3.5f, stats.Maximum);
        Assert.AreEqual(2.5, stats.Mean, 1e-9);
    }

    /// <summary>
    /// Verifies that decimal samples keep their exact values in the extrema while the moments are computed in
    /// <see cref="double" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenAccumulatingDecimals_ShouldTrackExactExtrema()
    {
        var stats = new RunningStatistics<decimal>();
        stats.Add(1.5m);
        stats.Add(2.5m);

        Assert.AreEqual(1.5m, stats.Minimum);
        Assert.AreEqual(2.5m, stats.Maximum);
        Assert.AreEqual(2.0, stats.Mean, 1e-12);
    }

    /// <summary>
    /// Verifies that adding a NaN sample throws <see cref="ArgumentException" /> and leaves the accumulator
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsNaN_ShouldThrowArgumentException()
    {
        var stats = new RunningStatistics<double>();
        stats.Add(1.0);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            stats.Add(double.NaN);
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(1L, stats.Count);
    }

    /// <summary>
    /// Verifies that adding an infinite sample throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenSampleIsPositiveInfinity_ShouldThrowArgumentException()
    {
        var stats = new RunningStatistics<double>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            stats.Add(double.PositiveInfinity);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" /> sample beyond the range of <see cref="double" /> throws
    /// <see cref="OverflowException" /> from the checked widening conversion.
    /// </summary>
    [TestMethod]
    public void Add_WhenBigIntegerSampleExceedsDoubleRange_ShouldThrowOverflowException()
    {
        var stats = new RunningStatistics<BigInteger>();

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            stats.Add(BigInteger.Pow(10, 400));
        });
    }

    /// <summary>
    /// Verifies that Welford's recurrence keeps the variance accurate for large closely clustered samples where the
    /// naive sum-of-squares formulation loses all precision.
    /// </summary>
    [TestMethod]
    public void Add_WhenSamplesAreLargeAndClustered_ShouldRemainNumericallyStable()
    {
        var stats = new RunningStatistics<double>();
        foreach (var offsetSample in new[] { 4.0, 7.0, 13.0, 16.0 })
            stats.Add(1e9 + offsetSample);

        Assert.AreEqual(30.0, stats.SampleVariance, 1e-6);
        Assert.AreEqual(1e9 + 10.0, stats.Mean, 1e-3);
    }
}

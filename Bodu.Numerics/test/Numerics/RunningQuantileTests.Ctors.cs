// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Verifies that a probability outside the open interval (0, 1) — including NaN — throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-0.1)]
    [DataRow(1.1)]
    [DataRow(double.NaN)]
    public void Ctor_WhenProbabilityIsOutOfRange_ShouldThrowArgumentOutOfRangeException(double probability)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new RunningQuantile<double>(probability);
        });

        Assert.AreEqual("probability", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an in-range probability is stored and reported unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0.05)]
    [DataRow(0.5)]
    [DataRow(0.95)]
    public void Ctor_WhenProbabilityIsInRange_ShouldReportProbability(double probability)
    {
        var estimator = new RunningQuantile<double>(probability);

        Assert.AreEqual(probability, estimator.Probability, 1e-15);
        Assert.IsTrue(estimator.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="RunningQuantile{T}.CreateMedian" /> yields an empty estimator targeting probability
    /// 0.5.
    /// </summary>
    [TestMethod]
    public void CreateMedian_WhenCalled_ShouldTargetProbabilityOfOneHalf()
    {
        var median = RunningQuantile<int>.CreateMedian();

        Assert.AreEqual(0.5, median.Probability);
        Assert.IsTrue(median.IsEmpty);
        Assert.AreEqual(0L, median.Count);
    }

    /// <summary>
    /// Verifies that the default value is a usable empty median estimator.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultValue_ShouldBeUsableMedianEstimator()
    {
        var estimator = default(RunningQuantile<double>);

        Assert.AreEqual(0.5, estimator.Probability);

        foreach (var sample in new[] { 5.0, 1.0, 3.0, 2.0, 4.0 })
            estimator.Add(sample);

        Assert.AreEqual(3.0, estimator.Estimate, 1e-12);
    }
}

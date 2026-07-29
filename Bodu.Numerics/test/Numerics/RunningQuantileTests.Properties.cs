// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Verifies that <see cref="RunningQuantile{T}.ToString" /> reports the probability, count, and current estimate
    /// once samples have been observed.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSamplesObserved_ShouldReportProbabilityCountAndEstimate()
    {
        var quantile = RunningQuantile<int>.CreateMedian();
        for (int sample = 1; sample <= 5; sample++)
            quantile.Add(sample);

        Assert.AreEqual("p = 0.5, Count = 5, Estimate = 3", quantile.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RunningQuantile{T}.ToString" /> omits the estimate before any sample is observed.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEmpty_ShouldReportZeroCountWithoutEstimate()
    {
        var quantile = new RunningQuantile<int>(0.5);

        Assert.AreEqual("p = 0.5, Count = 0", quantile.ToString());
    }
}

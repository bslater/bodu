// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for <see cref="RunningQuantile{T}" />.
/// </summary>
[TestClass]
public partial class RunningQuantileTests
{
    /// <summary>
    /// The worked example of Jain and Chlamtac (1985): twenty observations whose published P² median estimate is
    /// 4.44063.
    /// </summary>
    private static readonly double[] PaperObservations =
    [
        0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92,
        34.60, 10.28, 1.47, 0.40, 0.05, 11.39, 0.27, 0.42, 0.09, 11.37,
    ];

    /// <summary>
    /// Verifies that the published Jain and Chlamtac worked example reproduces the paper's median estimate
    /// (smoke test).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void RunningQuantile_WhenAccumulatingPaperObservations_ShouldReproducePublishedMedianEstimate()
    {
        var median = RunningQuantile<double>.CreateMedian();
        foreach (var observation in PaperObservations)
            median.Add(observation);

        Assert.AreEqual(20L, median.Count);
        Assert.AreEqual(4.44063, median.Value, 1e-4);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSumTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for <see cref="MovingSum{T}" />.
/// </summary>
[TestClass]
public partial class MovingSumTests
{
    /// <summary>
    /// Verifies that a rolling window reports the sum and mean of only its most recent samples (smoke test).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void MovingSum_WhenWindowRollsOver_ShouldReportSumOfLastNSamples()
    {
        var window = new MovingSum<double>(3);
        window.Add(1.0);
        window.Add(2.0);
        window.Add(3.0);
        window.Add(4.0);

        Assert.AreEqual(9.0, window.Sum, 1e-12);
        Assert.AreEqual(3.0, window.Mean, 1e-12);
        Assert.AreEqual(3, window.Count);
    }
}

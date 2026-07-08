// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMaxTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for <see cref="MovingMinMax{T}" />.
/// </summary>
[TestClass]
public partial class MovingMinMaxTests
{
    /// <summary>
    /// Verifies that a rolling window reports the extrema of only its most recent samples (smoke test).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void MovingMinMax_WhenWindowRollsOver_ShouldReportExtremaOfLastNSamples()
    {
        var window = new MovingMinMax<int>(3);
        window.Add(5);
        window.Add(1);
        window.Add(4);
        window.Add(2);

        Assert.AreEqual(1, window.Minimum);
        Assert.AreEqual(4, window.Maximum);
        Assert.AreEqual(3, window.Count);
    }
}

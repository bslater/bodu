// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSumTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class MovingSumTests
{
    /// <summary>
    /// Verifies that <see cref="MovingSum{T}.ToString" /> reports the capacity, count, and rolling sum.
    /// </summary>
    [TestMethod]
    public void ToString_WhenWindowHasSamples_ShouldReportCapacityCountAndSum()
    {
        var window = new MovingSum<int>(4);
        window.Add(2);
        window.Add(3);

        Assert.AreEqual("Capacity = 4, Count = 2, Sum = 5", window.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="MovingSum{T}.ToString" /> reports a zero count and sum for an empty window.
    /// </summary>
    [TestMethod]
    public void ToString_WhenWindowIsEmpty_ShouldReportZeroCountAndSum()
    {
        var window = new MovingSum<int>(3);

        Assert.AreEqual("Capacity = 3, Count = 0, Sum = 0", window.ToString());
    }
}

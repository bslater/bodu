// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMaxTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class MovingMinMaxTests
{
    /// <summary>
    /// Verifies that <see cref="MovingMinMax{T}.ToString" /> reports the capacity, count, and window extrema.
    /// </summary>
    [TestMethod]
    public void ToString_WhenWindowHasSamples_ShouldReportCapacityCountAndExtrema()
    {
        var window = new MovingMinMax<int>(3);
        window.Add(5);
        window.Add(2);

        Assert.AreEqual("Capacity = 3, Count = 2, Min = 2, Max = 5", window.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="MovingMinMax{T}.ToString" /> omits the extrema for an empty window.
    /// </summary>
    [TestMethod]
    public void ToString_WhenWindowIsEmpty_ShouldReportZeroCountWithoutExtrema()
    {
        var window = new MovingMinMax<int>(3);

        Assert.AreEqual("Capacity = 3, Count = 0", window.ToString());
    }
}

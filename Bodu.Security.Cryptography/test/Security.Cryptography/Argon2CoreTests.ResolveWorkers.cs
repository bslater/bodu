// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.ResolveWorkers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2CoreTests
{
    /// <summary>
    /// Verifies that the fill stays on the calling thread when the bound is one, when there is a single lane, and when
    /// segments are shorter than the default threshold of 64 blocks.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    /// <param name="lanes">The number of lanes.</param>
    /// <param name="segmentLength">The number of blocks in each segment.</param>
    [TestMethod]
    [DataRow(1, 8, 4096)]
    [DataRow(-1, 1, 4096)]
    [DataRow(64, 1, 4096)]
    [DataRow(-1, 8, 63)]
    [DataRow(4, 4, 63)]
    [DataRow(64, 16, 1)]
    public void ResolveWorkers_WhenTheFillShouldStayOnTheCallingThread_ShouldReturnOne(int maxDegreeOfParallelism, int lanes, int segmentLength)
    {
        var options = new Argon2Core.FillOptions(maxDegreeOfParallelism);

        Assert.AreEqual(1, options.ResolveWorkers(lanes, segmentLength));
    }

    /// <summary>
    /// Verifies that segments of at least the default threshold of 64 blocks, a 1 MiB matrix at four lanes, are divided
    /// among the smaller of the bound and the lane count.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    /// <param name="lanes">The number of lanes.</param>
    /// <param name="segmentLength">The number of blocks in each segment.</param>
    /// <param name="expected">The number of threads expected.</param>
    [TestMethod]
    [DataRow(2, 8, 64, 2)]
    [DataRow(4, 4, 64, 4)]
    [DataRow(4, 4, 256, 4)]
    [DataRow(3, 8, 4096, 3)]
    [DataRow(64, 4, 4096, 4)]
    [DataRow(int.MaxValue, 2, 1 << 20, 2)]
    public void ResolveWorkers_WhenSegmentsReachTheThreshold_ShouldReturnTheSmallerOfBoundAndLanes(int maxDegreeOfParallelism, int lanes, int segmentLength, int expected)
    {
        var options = new Argon2Core.FillOptions(maxDegreeOfParallelism);

        Assert.AreEqual(expected, options.ResolveWorkers(lanes, segmentLength));
    }

    /// <summary>
    /// Verifies that a bound of <c>-1</c> allows as many threads as there are processors, and never more than lanes.
    /// </summary>
    /// <param name="lanes">The number of lanes.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(1024)]
    public void ResolveWorkers_WhenBoundIsMinusOne_ShouldReturnTheSmallerOfProcessorCountAndLanes(int lanes)
    {
        var options = new Argon2Core.FillOptions(-1);

        Assert.AreEqual(Math.Min(Environment.ProcessorCount, lanes), options.ResolveWorkers(lanes, 4096));
    }

    /// <summary>
    /// Verifies that a lowered threshold divides even the shortest segments, which is how the tests drive small
    /// vectors through the threaded path.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenThresholdIsLowered_ShouldDivideShortSegments()
    {
        var options = new Argon2Core.FillOptions(4, minimumParallelSegmentLength: 1);

        Assert.AreEqual(4, options.ResolveWorkers(4, 2));
    }
}

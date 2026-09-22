// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.CapacityPartitioning.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the hot/warm/cold partition sums to the total capacity exactly for representative sizes.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(16)]
    [DataRow(99)]
    [DataRow(1000)]
    public void ComputePartition_WhenCapacitySupplied_ShouldSumSlicesToCapacityExactly(int capacity)
    {
        (int hot, int warm, int cold) = ConcurrentLruCache<string, int>.ComputePartition(capacity);

        Assert.AreEqual(capacity, hot + warm + cold, "The queue slices must sum to the total capacity.");
        Assert.IsTrue(hot >= 1, "The hot slice must always hold at least one entry.");
        Assert.IsTrue(warm >= 0);
        Assert.IsTrue(cold >= 0);
    }

    /// <summary>
    /// Verifies the degenerate partitions for tiny capacities and the thirds split with the remainder assigned to
    /// warm.
    /// </summary>
    [TestMethod]
    public void ComputePartition_WhenCapacityIsRepresentative_ShouldMatchExpectedShape()
    {
        Assert.AreEqual((1, 0, 0), ConcurrentLruCache<string, int>.ComputePartition(1));
        Assert.AreEqual((1, 0, 1), ConcurrentLruCache<string, int>.ComputePartition(2));
        Assert.AreEqual((1, 1, 1), ConcurrentLruCache<string, int>.ComputePartition(3));
        Assert.AreEqual((3, 4, 3), ConcurrentLruCache<string, int>.ComputePartition(10));
    }

    /// <summary>
    /// Verifies that a constructed cache exposes the computed partition through its queue capacity slices.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldExposeComputedPartition()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 10);

        Assert.AreEqual(3, cache.HotCapacity);
        Assert.AreEqual(4, cache.WarmCapacity);
        Assert.AreEqual(3, cache.ColdCapacity);
    }

    /// <summary>
    /// Verifies that after maintenance converges, every queue occupancy is within its capacity slice.
    /// </summary>
    [TestMethod]
    public void RunMaintenance_WhenQueuesOverfilled_ShouldConvergeEveryQueueWithinItsSlice()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 9);

        for (int i = 0; i < 100; i++)
            cache.Add(i, i);

        cache.RunMaintenance();

        Assert.IsTrue(cache.HotCount <= cache.HotCapacity, $"Hot occupancy {cache.HotCount} exceeded its slice {cache.HotCapacity}.");
        Assert.IsTrue(cache.WarmCount <= cache.WarmCapacity, $"Warm occupancy {cache.WarmCount} exceeded its slice {cache.WarmCapacity}.");
        Assert.IsTrue(cache.ColdCount <= cache.ColdCapacity, $"Cold occupancy {cache.ColdCount} exceeded its slice {cache.ColdCapacity}.");
        Assert.IsTrue(cache.Count <= cache.Capacity, $"Count {cache.Count} exceeded the capacity after convergence.");
    }
}

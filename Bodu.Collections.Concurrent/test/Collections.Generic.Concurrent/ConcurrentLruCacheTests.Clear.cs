// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that clearing removes every entry and empties the occupancy counters.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesExist_ShouldRemoveEverything()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        cache.Add("b", 2);

        cache.Clear();

        Assert.AreEqual(0, cache.Count);
        Assert.IsTrue(cache.IsEmpty);
        Assert.AreEqual(0, cache.ApproximateCount);
        Assert.IsFalse(cache.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that clearing resets the telemetry counters.
    /// </summary>
    [TestMethod]
    public void Clear_WhenTelemetryAccumulated_ShouldResetCounters()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 3);
        for (int i = 0; i < 20; i++)
            cache.Add(i, i);
        Assert.IsFalse(cache.TryGetValue(-1, out _));
        cache.RunMaintenance();
        Assert.AreNotEqual(0, cache.EvictionCount);
        Assert.AreNotEqual(0, cache.MissCount);

        cache.Clear();

        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);
        Assert.AreEqual(0, cache.EvictionCount);
        Assert.AreEqual(0.0, cache.HitRatio);
    }

    /// <summary>
    /// Verifies that clearing does not raise <see cref="ConcurrentLruCache{TKey, TValue}.ItemEvicted" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesExist_ShouldNotRaiseItemEvicted()
    {
        var cache = new ConcurrentLruCache<string, int>();
        int evictedCallbacks = 0;
        cache.ItemEvicted += (_, _) => evictedCallbacks++;
        cache.Add("a", 1);
        cache.Add("b", 2);

        cache.Clear();

        Assert.AreEqual(0, evictedCallbacks);
    }

    /// <summary>
    /// Verifies that the cache is fully usable after a clear.
    /// </summary>
    [TestMethod]
    public void Clear_WhenReusedAfterClear_ShouldBehaveAsEmptyCache()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 4);
        cache.Add("a", 1);
        cache.Clear();

        cache.Add("x", 10);
        cache.RunMaintenance();

        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.TryGetValue("x", out int value));
        Assert.AreEqual(10, value);
    }

    /// <summary>
    /// Verifies that when an entry has left the backing dictionary but its node is not yet marked dead - a remover
    /// between its two steps - <see cref="ConcurrentLruCache{TKey, TValue}.Clear" /> neither sees it in the sweep nor
    /// drops it from its queue: the drain re-enqueues the live node, which keeps its occupancy slot until the remover
    /// marks it and a later pass drains it, while the swept entries' dead nodes are dropped.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRemovedEntryIsNotYetMarked_ShouldKeepItsLiveNodeQueuedUntilDrained()
    {
        // Capacity 4 partitions as hot 1 / warm 2 / cold 1: after the two adds, 'a' sits in cold and 'b' in hot.
        var cache = new ConcurrentLruCache<string, int>(capacity: 4);
        cache.Add("a", 1);
        cache.Add("b", 2);
        Assert.AreEqual(1, cache.HotCount);
        Assert.AreEqual(1, cache.ColdCount);
        Assert.IsNull(cache.RemoveWithoutMarking("absent"), "A remover for an absent key has no node to take out.");
        object? node = cache.RemoveWithoutMarking("b");
        Assert.IsNotNull(node);
        Assert.AreEqual(1, cache.Count);

        cache.Clear();

        Assert.AreEqual(0, cache.Count);
        Assert.IsTrue(cache.IsEmpty);
        Assert.AreEqual(1, cache.HotCount, "The unmarked node must survive the drain and keep its hot slot.");
        Assert.AreEqual(0, cache.ColdCount, "The swept entry's dead node must have been dropped from cold.");

        ConcurrentLruCache<string, int>.MarkNodeRemoved(node);
        cache.Add("c", 3);
        cache.Add("d", 4);
        cache.RunMaintenance();

        Assert.AreEqual(2, cache.Count);
        Assert.AreEqual(0, cache.EvictionCount, "A node that left the dictionary before it was marked must not count as an eviction.");
        AssertContainsExactly(cache, ("c", 3), ("d", 4));
        Assert.AreEqual(cache.Count, cache.HotCount + cache.WarmCount + cache.ColdCount, "Once drained, the queues hold only live entries.");
    }
}

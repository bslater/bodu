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
}

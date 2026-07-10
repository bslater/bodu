// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Telemetry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that only genuine lookups contribute to the hit/miss counters: adds, probes, snapshots, and removals
    /// are neutral.
    /// </summary>
    [TestMethod]
    public void Telemetry_WhenMixedOperationsRun_ShouldCountOnlyLookups()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);                                  // neutral
        Assert.IsTrue(cache.TryGetValue("a", out _));       // hit
        _ = cache.GetOrAdd("a", 9);                         // hit
        _ = cache.GetOrAdd("b", 2);                         // miss (add path)
        Assert.IsFalse(cache.TryGetValue("x", out _));      // miss
        Assert.IsTrue(cache.ContainsKey("a"));              // neutral
        _ = cache.ToArray();                                // neutral
        Assert.IsTrue(cache.TryRemove("b", out _));         // neutral

        Assert.AreEqual(2, cache.HitCount);
        Assert.AreEqual(2, cache.MissCount);
        Assert.AreEqual(0.5, cache.HitRatio, 1e-12);
    }

    /// <summary>
    /// Verifies that the hit and miss counters lose no increments under parallel readers hammering the striped
    /// counters.
    /// </summary>
    [TestMethod]
    public void Telemetry_WhenReadInParallel_ShouldNotLoseIncrements()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 64);
        for (int i = 0; i < 16; i++)
            cache.Add(i, i);

        const int workers = 8;
        const int readsPerWorker = 5_000;

        Parallel.For(0, workers, _ =>
        {
            for (int i = 0; i < readsPerWorker; i++)
            {
                Assert.IsTrue(cache.TryGetValue(i % 16, out _));
                Assert.IsFalse(cache.TryGetValue(1_000 + (i % 16), out _));
            }
        });

        Assert.AreEqual(workers * (long)readsPerWorker, cache.HitCount);
        Assert.AreEqual(workers * (long)readsPerWorker, cache.MissCount);
        Assert.AreEqual(0.5, cache.HitRatio, 1e-12);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentLruCache{TKey, TValue}.EvictionCount" /> counts only capacity evictions,
    /// not explicit removals or replacements.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenMixedRemovalsOccur_ShouldCountOnlyEvictions()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 3);

        for (int i = 0; i < 12; i++)
            cache.Add(i, i);
        cache.RunMaintenance();
        long capacityEvictions = cache.EvictionCount;
        Assert.IsTrue(capacityEvictions > 0);

        int resident = cache.ToArray()[0].Key;
        Assert.IsTrue(cache.TryRemove(resident, out _));   // explicit removal: +0
        cache.Add(100, 100);                               // fills the freed logical slot
        cache.Add(100, 101);                               // replacement: +0
        cache.RunMaintenance();

        Assert.AreEqual(capacityEvictions, cache.EvictionCount, "Explicit removals and replacements must not count as evictions.");
    }
}

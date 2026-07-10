// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Telemetry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TotalTouches" /> counts successful lookups,
    /// touches, and GetOrAdd hits, but not misses, containment probes, or adds.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenMixedOperationsRun_ShouldCountOnlySuccessfulAccesses()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.TryGetValue("a", out _));   // +1
        Assert.IsTrue(dictionary.Touch("a"));                // +1
        _ = dictionary.GetOrAdd("a", 99);                    // +1 (hit)
        _ = dictionary["a"];                                 // +1 (indexer get)
        Assert.IsFalse(dictionary.TryGetValue("x", out _));  // miss
        Assert.IsTrue(dictionary.ContainsKey("a"));          // probe, not a touch
        _ = dictionary.GetOrAdd("b", 2);                     // add path, not a touch

        Assert.AreEqual(4, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.EvictionCount" /> accumulates capacity and
    /// expiry evictions but not explicit removals.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenMixedRemovalsOccur_ShouldCountOnlyEvictions()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time);
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut, expiration);

        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);                              // capacity eviction of 'a': +1

        Assert.IsTrue(dictionary.TryRemove("b", out _));     // explicit removal: +0
        dictionary.Add("mortal", 4, TimeSpan.FromMinutes(1));
        time.Advance(TimeSpan.FromMinutes(2));
        Assert.AreEqual(1, dictionary.RemoveExpired());      // expiry eviction: +1

        Assert.AreEqual(2, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that both telemetry counters accumulate across parallel workers without losing increments.
    /// </summary>
    [TestMethod]
    public void Telemetry_WhenAccessedInParallel_ShouldNotLoseIncrements()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 64);
        for (int i = 0; i < 32; i++)
            dictionary.Add(i, i);

        const int workers = 8;
        const int readsPerWorker = 1_000;

        Parallel.For(0, workers, _ =>
        {
            for (int i = 0; i < readsPerWorker; i++)
                Assert.IsTrue(dictionary.TryGetValue(i % 32, out _));
        });

        Assert.AreEqual(workers * (long)readsPerWorker, dictionary.TotalTouches);
    }
}

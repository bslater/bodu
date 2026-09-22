// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.ItemEvicted.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that a capacity eviction raises the event with the evicted key and value.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenCapacityEvictionOccurs_ShouldRaiseWithEvictedEntry()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<(string Key, int Value)>();
        cache.ItemEvicted += (key, value) => evicted.Add((key, value));

        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.Add("c", 3);
        cache.RunMaintenance();

        Assert.HasCount(1, evicted, "Flow-through pressure on a capacity-3 cache must evict exactly one of the three unread adds.");
        Assert.AreEqual(("a", 1), evicted[0], "The oldest unread entry must be the first eviction.");
    }

    /// <summary>
    /// Verifies that the event is raised after the eviction has been committed: the evicted key is already absent when
    /// the handler runs.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerRuns_ShouldObserveEntryAlreadyRemoved()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        bool? containedDuringHandler = null;
        cache.ItemEvicted += (key, _) => containedDuringHandler ??= cache.ContainsKey(key);

        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.Add("c", 3);
        cache.RunMaintenance();

        Assert.IsFalse(containedDuringHandler, "The evicted entry must already be gone when the handler observes the cache.");
    }

    /// <summary>
    /// Verifies that handlers run outside the maintenance lock: a handler can call back into the cache without
    /// deadlocking or corrupting state.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerMutatesCache_ShouldNotDeadlockOrCorrupt()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.ItemEvicted += (key, value) =>
        {
            // Only react to the first, known victim so the callback cannot cascade indefinitely.
            if (key == "a")
                _ = cache.TryAdd("evicted:a", value);
        };

        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.Add("c", 3);
        cache.RunMaintenance();

        Assert.IsTrue(cache.ContainsKey("evicted:a"), "The re-entrant add from the handler must have been applied.");
    }

    /// <summary>
    /// Verifies that a throwing handler is suppressed and does not prevent later subscribers from being notified.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerThrows_ShouldSuppressAndNotifyRemainingSubscribers()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        int secondHandlerCalls = 0;
        cache.ItemEvicted += (_, _) => throw new InvalidOperationException("handler failure");
        cache.ItemEvicted += (_, _) => secondHandlerCalls++;

        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.Add("c", 3);
        cache.RunMaintenance();

        Assert.AreEqual(1, secondHandlerCalls, "The second subscriber must be notified despite the first throwing.");
    }

    /// <summary>
    /// Verifies that a cache with no subscribers still evicts correctly and tracks
    /// <see cref="ConcurrentLruCache{TKey, TValue}.EvictionCount" />.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenNoSubscribers_ShouldStillEvictAndCount()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 3);

        for (int i = 0; i < 10; i++)
            cache.Add(i, i);
        cache.RunMaintenance();

        Assert.AreEqual(10 - cache.Count, cache.EvictionCount);
        Assert.IsTrue(cache.EvictionCount > 0);
    }
}

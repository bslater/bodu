// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.TryRemove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that removing an existing key returns <see langword="true" /> and surfaces the removed value.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 42);

        Assert.IsTrue(cache.TryRemove("a", out int value));
        Assert.AreEqual(42, value);
        Assert.AreEqual(0, cache.Count);
        Assert.IsFalse(cache.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that removing a missing key returns <see langword="false" /> and the default value.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.IsFalse(cache.TryRemove("missing", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.TryRemove(null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an explicit removal is not an eviction: no event is raised and the eviction count is unchanged.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyExists_ShouldNotCountAsEviction()
    {
        var cache = new ConcurrentLruCache<string, int>();
        int evictedCallbacks = 0;
        cache.ItemEvicted += (_, _) => evictedCallbacks++;
        cache.Add("a", 1);

        Assert.IsTrue(cache.TryRemove("a", out _));
        cache.RunMaintenance();

        Assert.AreEqual(0, evictedCallbacks);
        Assert.AreEqual(0, cache.EvictionCount);
    }

    /// <summary>
    /// Verifies that a removed key is immediately reusable, and the stale queued node is drained rather than
    /// resurrecting the old entry.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyReAddedAfterRemoval_ShouldStoreFreshEntry()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 4);
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryRemove("a", out _));

        cache.Add("a", 2);
        cache.RunMaintenance();

        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(2, value);
        Assert.AreEqual(1, cache.Count);
    }

    /// <summary>
    /// Verifies that maintenance drains the dead queue node left behind by an explicit removal, reconciling the
    /// approximate occupancy with the live count.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenMaintenanceRuns_ShouldDrainDeadQueueNode()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 6);
        for (int i = 0; i < 6; i++)
            cache.Add(i, i);

        // Flow-through pressure has already evicted some of the unread adds; remove three still-resident keys so the
        // dead nodes we create are the ones under test.
        int[] residents = cache.ToArray().Take(3).Select(pair => pair.Key).ToArray();
        Assert.HasCount(3, residents, "At least three of the six adds must still be resident.");
        foreach (int resident in residents)
            Assert.IsTrue(cache.TryRemove(resident, out _));

        // Push new keys through so every queue drains its dead nodes.
        for (int i = 100; i < 110; i++)
            cache.Add(i, i);
        cache.RunMaintenance();

        Assert.IsTrue(cache.ApproximateCount <= cache.Capacity, "Dead nodes must not permanently inflate the occupancy counters.");
    }
}

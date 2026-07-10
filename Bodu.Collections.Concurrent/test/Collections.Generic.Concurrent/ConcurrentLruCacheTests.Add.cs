// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that adding a new key stores the entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldStoreEntry()
    {
        var cache = new ConcurrentLruCache<string, int>();

        cache.Add("a", 1);

        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that adding an existing key replaces the value without changing the count and without throwing.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyExists_ShouldReplaceValueWithoutChangingCount()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        cache.Add("a", 2);

        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that adding a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.Add(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that replacing a key is not an eviction: no event is raised and the eviction count is unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyReplaced_ShouldNotCountAsEviction()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 8);
        int evictedCallbacks = 0;
        cache.ItemEvicted += (_, _) => evictedCallbacks++;
        cache.Add("a", 1);

        cache.Add("a", 2);
        cache.RunMaintenance();

        Assert.AreEqual(0, evictedCallbacks);
        Assert.AreEqual(0, cache.EvictionCount);
    }

    /// <summary>
    /// Verifies that sustained adds of new keys keep the converged count bounded by the capacity via eviction.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyNewKeysAdded_ShouldEvictToStayWithinCapacity()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 12);

        for (int i = 0; i < 200; i++)
            cache.Add(i, i);

        cache.RunMaintenance();

        Assert.IsTrue(cache.Count <= 12, $"Count {cache.Count} exceeded the capacity after convergence.");
        Assert.AreEqual(200 - cache.Count, cache.EvictionCount, "Every admitted entry beyond the survivors must be one eviction.");
    }

    /// <summary>
    /// Verifies that replacing a key restarts its recency life: the fresh entry re-enters the hot queue and survives
    /// pressure that would have evicted the stale original.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyReplaced_ShouldRestartRecencyLife()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.RunMaintenance();

        cache.Add("a", 10);
        cache.Add("c", 3);
        cache.RunMaintenance();

        Assert.IsTrue(cache.ContainsKey("a"), "The replaced entry re-entered hot and must have outlived the older 'b'.");
        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(10, value);
    }
}

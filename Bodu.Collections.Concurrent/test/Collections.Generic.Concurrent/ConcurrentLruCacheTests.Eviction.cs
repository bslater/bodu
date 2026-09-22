// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Eviction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that entries that are never read are evicted in insertion order — the pseudo-LRU degenerates to FIFO
    /// flow-through when nothing is accessed.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenEntriesNeverRead_ShouldEvictInInsertionOrder()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);

        foreach (string key in new[] { "a", "b", "c", "d", "e" })
        {
            cache.Add(key, 0);
            cache.RunMaintenance();
        }

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, evicted, "Unread entries must flow through hot and cold in insertion order.");
    }

    /// <summary>
    /// Verifies that a read while hot promotes the entry to warm, protecting it from the flow-through that evicts its
    /// unread peers.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenEntryReadWhileHot_ShouldPromoteToWarmAndSurvive()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryGetValue("a", out _));

        for (int i = 0; i < 8; i++)
        {
            cache.Add($"filler{i}", i);
            cache.RunMaintenance();
        }

        Assert.IsTrue(cache.ContainsKey("a"), "The accessed entry must sit in warm, out of the flow-through path.");
        Assert.AreEqual(1, cache.WarmCount, "Exactly the one accessed entry must occupy warm.");
    }

    /// <summary>
    /// Verifies that a read while cold resurrects the entry to warm on the next maintenance pass instead of evicting
    /// it.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenEntryReadWhileCold_ShouldResurrectToWarm()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.RunMaintenance();

        // 'a' has been demoted to cold by 'b''s arrival; read it there, then apply pressure.
        Assert.AreEqual(1, cache.ColdCount);
        Assert.IsTrue(cache.TryGetValue("a", out _));

        for (int i = 0; i < 6; i++)
        {
            cache.Add($"filler{i}", i);
            cache.RunMaintenance();
        }

        Assert.IsTrue(cache.ContainsKey("a"), "The cold-read entry must have been resurrected to warm, not evicted.");
    }

    /// <summary>
    /// Verifies that an unread warm entry is demoted toward eviction once warm overflows, while re-read warm entries
    /// recycle and survive.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenWarmOverflows_ShouldDemoteUnreadAndRecycleRead()
    {
        // Capacity 10 partitions as hot 3 / warm 4 / cold 3.
        var cache = new ConcurrentLruCache<string, int>(capacity: 10);

        // Promote five entries into warm (one more than its slice): add, read, cycle each into warm.
        for (int i = 0; i < 5; i++)
        {
            cache.Add($"w{i}", i);
            Assert.IsTrue(cache.TryGetValue($"w{i}", out _));
        }

        // Push filler through hot so the accessed entries drain into warm.
        for (int i = 0; i < 6; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();

        // Keep w1..w4 alive; leave w0 unread so the next overflow demotes it.
        for (int i = 1; i < 5; i++)
            Assert.IsTrue(cache.TryGetValue($"w{i}", out _));

        for (int i = 10; i < 20; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();
        for (int i = 20; i < 30; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();

        for (int i = 1; i < 5; i++)
            Assert.IsTrue(cache.ContainsKey($"w{i}"), $"The re-read warm entry w{i} must have recycled and survived.");
        Assert.IsFalse(cache.ContainsKey("w0"), "The unread warm entry must have been demoted and eventually evicted.");
    }

    /// <summary>
    /// Verifies that eviction accounting reconciles exactly: every admitted unique key is either resident or was
    /// evicted exactly once.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenSequentialAddsConverge_ShouldReconcileAdmissionsWithEvictions()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 9);
        var evicted = new List<int>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);

        const int totalAdds = 300;
        for (int i = 0; i < totalAdds; i++)
            cache.Add(i, i);
        cache.RunMaintenance();

        Assert.AreEqual(totalAdds - cache.Count, cache.EvictionCount);
        Assert.AreEqual(cache.EvictionCount, evicted.Count, "Every eviction must produce exactly one event callback.");
        Assert.AreEqual(evicted.Count, evicted.Distinct().Count(), "No key may be evicted twice.");
    }
}

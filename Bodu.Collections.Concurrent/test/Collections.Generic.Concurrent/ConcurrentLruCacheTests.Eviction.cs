// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Eviction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that entries that are never read are evicted in insertion order - the pseudo-LRU degenerates to FIFO
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

    /// <summary>
    /// Verifies that when warm overflows and its head has been read since it arrived there, the head recycles to the
    /// warm tail with its flag cleared instead of demoting, so its unread neighbor is the one demoted and later
    /// evicted.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenWarmHeadReadBeforeOverflow_ShouldRecycleHeadAndDemoteUnreadNeighbor()
    {
        // Capacity 3 partitions as hot 1 / warm 1 / cold 1.
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);

        // 'a' is read while hot, so the arrival of 'b' promotes it to warm.
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryGetValue("a", out _));
        cache.Add("b", 2);
        Assert.AreEqual(1, cache.WarmCount);

        // Read both: 'a' is flagged in warm and 'b' in hot. The arrival of 'c' promotes 'b', overflowing warm; the
        // drain recycles the read head 'a' behind 'b' and then demotes the unread 'b' to cold.
        Assert.IsTrue(cache.TryGetValue("a", out _));
        Assert.IsTrue(cache.TryGetValue("b", out _));
        cache.Add("c", 3);
        Assert.AreEqual(1, cache.WarmCount);
        Assert.AreEqual(1, cache.ColdCount);
        Assert.HasCount(0, evicted, "Recycling and demotion alone must not evict.");

        // The arrival of 'd' demotes 'c' into cold, overflowing it; the demoted 'b', not the recycled 'a', is evicted.
        cache.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "b" }, evicted, "The recycled warm head must survive; its demoted neighbor is the victim.");
        AssertContainsExactly(cache, ("a", 1), ("c", 3), ("d", 4));
    }

    /// <summary>
    /// Verifies that when the cold drain resurrects a read entry instead of evicting it, so the policy cycling alone
    /// leaves the cache over capacity, the capacity enforcement evicts from the cold head regardless of its accessed
    /// flag and the capacity contract holds.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenColdResurrectionLeavesCacheOverCapacity_ShouldEnforceCapacityFromColdHead()
    {
        // Capacity 2 partitions as hot 1 / warm 0 / cold 1.
        var cache = new ConcurrentLruCache<string, int>(capacity: 2);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);

        cache.Add("a", 1);
        cache.Add("b", 2);
        Assert.AreEqual(1, cache.ColdCount, "The arrival of 'b' must have demoted the unread 'a' to cold.");

        // Read 'a' in cold. The arrival of 'c' demotes 'b' to cold behind it; the cold drain resurrects the read 'a'
        // to warm rather than evicting it, which leaves three live entries in a capacity-2 cache.
        Assert.IsTrue(cache.TryGetValue("a", out _));
        cache.Add("c", 3);

        CollectionAssert.AreEqual(new[] { "b" }, evicted, "Capacity enforcement must evict the unread cold head left behind by the resurrection.");
        Assert.AreEqual(2, cache.Count);
        Assert.AreEqual(1, cache.EvictionCount);
        AssertContainsExactly(cache, ("a", 1), ("c", 3));
    }

    /// <summary>
    /// Verifies that when the live count exceeds the capacity while the cold queue is empty - in-flight writers hold
    /// entries no queue has received yet - the hard capacity enforcement demotes the warm head to cold and evicts it,
    /// so the capacity contract holds without waiting for the writers to deliver.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLiveCountExceedsCapacityAndColdQueueIsEmpty_ShouldDemoteFromWarmAndEvict()
    {
        // Capacity 3 partitions as hot 1 / warm 1 / cold 1.
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryGetValue("a", out _));
        cache.Add("b", 2);
        Assert.AreEqual(1, cache.WarmCount, "The accessed entry must have been promoted to warm by the second add.");
        Assert.AreEqual(0, cache.ColdCount);
        Assert.IsTrue(cache.AddInFlight("c", 3));
        Assert.IsTrue(cache.AddInFlight("d", 4));
        Assert.AreEqual(4, cache.ApproximateCount);

        cache.RunMaintenance();

        CollectionAssert.AreEqual(new[] { "a" }, evicted, "With cold empty, the warm head must be demoted and evicted.");
        Assert.AreEqual(3, cache.ApproximateCount);
        Assert.AreEqual(0, cache.WarmCount);
        AssertContainsExactly(cache, ("b", 2), ("c", 3), ("d", 4));
    }

    /// <summary>
    /// Verifies that when the live count exceeds the capacity while both the cold and the warm queues are empty, the
    /// hard capacity enforcement demotes the hot head to cold and evicts it.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLiveCountExceedsCapacityAndOnlyHotQueueHoldsEntries_ShouldDemoteFromHotAndEvict()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);
        cache.Add("a", 1);
        Assert.AreEqual(1, cache.HotCount);
        Assert.IsTrue(cache.AddInFlight("b", 2));
        Assert.IsTrue(cache.AddInFlight("c", 3));
        Assert.IsTrue(cache.AddInFlight("d", 4));

        cache.RunMaintenance();

        CollectionAssert.AreEqual(new[] { "a" }, evicted, "With cold and warm empty, the hot head must be demoted and evicted.");
        Assert.AreEqual(3, cache.ApproximateCount);
        Assert.AreEqual(0, cache.HotCount);
        AssertContainsExactly(cache, ("b", 2), ("c", 3), ("d", 4));
    }

    /// <summary>
    /// Verifies that when the live count still exceeds the capacity after every queue has been drained - the excess
    /// is held entirely by in-flight writers - the hard capacity enforcement stops rather than spinning, and that the
    /// maintenance run after the writers deliver brings the cache back within its capacity.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenEveryQueueIsEmptyWhileWritersAreInFlight_ShouldStopAndConvergeOnceDelivered()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);
        cache.Add("a", 1);
        foreach (string key in new[] { "b", "c", "d", "e" })
            Assert.IsTrue(cache.AddInFlight(key, 0));
        Assert.IsFalse(cache.AddInFlight("a", 0), "A writer for a key that is already present adds nothing.");

        cache.RunMaintenance();

        CollectionAssert.AreEqual(new[] { "a" }, evicted, "Only the delivered entry can be evicted while the rest are in flight.");
        Assert.AreEqual(4, cache.ApproximateCount, "The undelivered entries stay until their writers deliver them.");
        Assert.AreEqual(0, cache.HotCount);

        Assert.IsFalse(cache.DeliverInFlight("a"), "The evicted entry has no node left to deliver.");
        foreach (string key in new[] { "b", "c", "d", "e" })
            Assert.IsTrue(cache.DeliverInFlight(key));
        cache.RunMaintenance();

        Assert.IsTrue(cache.Count <= cache.Capacity, $"Count {cache.Count} must be within the capacity once the writers have delivered.");
        Assert.AreEqual(cache.Count, cache.ApproximateCount);
        Assert.AreEqual(3, evicted.Count, "The delivered entries flow through the queues and the unread excess is evicted.");
    }

    /// <summary>
    /// Verifies that the hard capacity enforcement drops a dead node it dequeues from hot while refilling the empty
    /// cold queue - an entry removed after it was queued - without counting it as an eviction, and that the cache
    /// converges once the in-flight writers deliver.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenHardEnforcementDequeuesRemovedEntryFromHot_ShouldDropItWithoutEvicting()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        var evicted = new List<string>();
        cache.ItemEvicted += (key, _) => evicted.Add(key);
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryRemove("a", out _));
        Assert.AreEqual(1, cache.HotCount, "The removed entry's dead node keeps its hot slot until a pass drains it.");
        foreach (string key in new[] { "b", "c", "d", "e" })
            Assert.IsTrue(cache.AddInFlight(key, 0));

        cache.RunMaintenance();

        Assert.IsEmpty(evicted, "A removed entry is dropped, not evicted.");
        Assert.AreEqual(0, cache.HotCount, "The dead node must have been drained from hot.");
        Assert.AreEqual(0, cache.EvictionCount);
        Assert.AreEqual(4, cache.ApproximateCount);

        foreach (string key in new[] { "b", "c", "d", "e" })
            Assert.IsTrue(cache.DeliverInFlight(key));
        cache.RunMaintenance();

        Assert.IsTrue(cache.Count <= cache.Capacity, $"Count {cache.Count} must be within the capacity once the writers have delivered.");
        Assert.IsFalse(cache.ContainsKey("a"));
    }
}

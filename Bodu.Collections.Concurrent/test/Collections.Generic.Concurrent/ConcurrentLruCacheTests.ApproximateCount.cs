// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.ApproximateCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the approximate count is zero for a new cache.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenEmpty_ShouldBeZero()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.AreEqual(0, cache.ApproximateCount);
    }

    /// <summary>
    /// Verifies that the approximate count matches the exact count when no dead nodes are pending.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenNoDeadNodesPending_ShouldMatchExactCount()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 64);
        for (int i = 0; i < 30; i++)
            cache.Add(i, i);

        Assert.AreEqual(cache.Count, cache.ApproximateCount);
    }

    /// <summary>
    /// Verifies that the approximate count tracks explicit removals immediately: it is a live-entry counter, not a
    /// queue-occupancy sum, so dead queue nodes do not inflate it.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenEntriesRemoved_ShouldTrackLiveEntries()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 64);
        for (int i = 0; i < 10; i++)
            cache.Add(i, i);

        for (int i = 0; i < 5; i++)
            Assert.IsTrue(cache.TryRemove(i, out _));

        Assert.AreEqual(5, cache.Count);
        Assert.AreEqual(5, cache.ApproximateCount, "The live-entry counter must reflect removals without waiting for maintenance.");
    }
}

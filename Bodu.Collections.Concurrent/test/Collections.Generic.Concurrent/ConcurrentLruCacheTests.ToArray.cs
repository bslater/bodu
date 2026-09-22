// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that an empty cache snapshots to an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenEmpty_ShouldReturnEmptyArray()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.IsEmpty(cache.ToArray());
    }

    /// <summary>
    /// Verifies that the snapshot contains exactly the stored entries.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenEntriesExist_ShouldContainExactlyStoredEntries()
    {
        var cache = new ConcurrentLruCache<int, string>(capacity: 32);
        for (int i = 0; i < 10; i++)
            cache.Add(i, $"v{i}");

        KeyValuePair<int, string>[] snapshot = cache.ToArray();

        Assert.HasCount(10, snapshot);
        foreach (KeyValuePair<int, string> pair in snapshot)
            Assert.AreEqual($"v{pair.Key}", pair.Value);
    }

    /// <summary>
    /// Verifies that the snapshot is detached: mutations made after the call do not alter the returned array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenMutatedAfterSnapshot_ShouldNotReflectMutations()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        KeyValuePair<string, int>[] snapshot = cache.ToArray();
        cache.Add("b", 2);
        Assert.IsTrue(cache.TryRemove("a", out _));

        Assert.HasCount(1, snapshot);
        Assert.AreEqual("a", snapshot[0].Key);
    }

    /// <summary>
    /// Verifies that snapshotting is pure: it neither marks entries accessed nor contributes to telemetry.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldNotTouchEntriesOrTelemetry()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);

        _ = cache.ToArray();

        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);

        // The snapshot must not have set 'a''s accessed flag: it flushes with the fillers.
        for (int i = 0; i < 10; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();

        Assert.IsFalse(cache.ContainsKey("a"));
    }
}

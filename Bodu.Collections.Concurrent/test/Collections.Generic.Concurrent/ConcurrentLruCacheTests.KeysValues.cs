// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.KeysValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the keys and values snapshots contain exactly the stored entries.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenEntriesExist_ShouldContainExactlyStoredEntries()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        cache.Add("b", 2);

        IReadOnlyCollection<string> keys = cache.Keys;
        IReadOnlyCollection<int> values = cache.Values;

        Assert.HasCount(2, keys);
        Assert.Contains("a", keys);
        Assert.Contains("b", keys);
        Assert.HasCount(2, values);
        Assert.Contains(1, values);
        Assert.Contains(2, values);
    }

    /// <summary>
    /// Verifies that the keys and values collections are snapshots, not live views.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenMutatedAfterRead_ShouldNotReflectMutations()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        IReadOnlyCollection<string> keys = cache.Keys;
        IReadOnlyCollection<int> values = cache.Values;
        cache.Add("b", 2);

        Assert.HasCount(1, keys);
        Assert.HasCount(1, values);
    }

    /// <summary>
    /// Verifies that the keys and values snapshots of an empty cache are empty.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenEmpty_ShouldBeEmpty()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.IsEmpty(cache.Keys);
        Assert.IsEmpty(cache.Values);
    }

    /// <summary>
    /// Verifies that reading the snapshots contributes nothing to the telemetry counters.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenRead_ShouldNotContributeToTelemetry()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        _ = cache.Keys;
        _ = cache.Values;

        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);
    }
}

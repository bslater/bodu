// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that a lookup of an existing key returns <see langword="true" /> and the stored value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 42);

        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that a lookup of a missing key returns <see langword="false" /> and the default value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.IsFalse(cache.TryGetValue("missing", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.TryGetValue(null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a hit marks the entry accessed, protecting it from the eviction pass that claims its unread
    /// peers.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenHit_ShouldProtectEntryFromEviction()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);
        Assert.IsTrue(cache.TryGetValue("a", out _));

        // Push enough unread keys through to flush every unprotected entry out of the cache.
        for (int i = 0; i < 10; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();

        Assert.IsTrue(cache.ContainsKey("a"), "The read entry must have been promoted to warm and survived the flush.");
    }

    /// <summary>
    /// Verifies that hits and misses are recorded in the telemetry counters.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenCalled_ShouldRecordHitsAndMisses()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        Assert.IsTrue(cache.TryGetValue("a", out _));
        Assert.IsTrue(cache.TryGetValue("a", out _));
        Assert.IsFalse(cache.TryGetValue("x", out _));

        Assert.AreEqual(2, cache.HitCount);
        Assert.AreEqual(1, cache.MissCount);
        Assert.AreEqual(2.0 / 3.0, cache.HitRatio, 1e-12);
    }
}

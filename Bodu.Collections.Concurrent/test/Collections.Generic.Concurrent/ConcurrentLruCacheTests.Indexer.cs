// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the indexer getter returns the stored value for an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 42);

        Assert.AreEqual(42, cache["a"]);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowKeyNotFoundException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = cache["missing"];
        });

        Assert.IsTrue(ex.Message.Contains("missing", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the indexer setter adds a new entry or replaces an existing one.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSet_ShouldAddOrReplace()
    {
        var cache = new ConcurrentLruCache<string, int>();

        cache["a"] = 1;
        cache["a"] = 2;

        Assert.AreEqual(1, cache.Count);
        Assert.AreEqual(2, cache["a"]);
    }

    /// <summary>
    /// Verifies that the indexer getter records telemetry like
    /// <see cref="ConcurrentLruCache{TKey, TValue}.TryGetValue" /> — a hit on success and a miss before throwing.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenRead_ShouldRecordTelemetry()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        _ = cache["a"];
        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = cache["missing"];
        });

        Assert.AreEqual(1, cache.HitCount);
        Assert.AreEqual(1, cache.MissCount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key on either accessor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache[null!];
        });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache[null!] = 1;
        });
    }
}

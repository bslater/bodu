// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.TryAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that adding a new key returns <see langword="true" /> and stores the entry.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNew_ShouldReturnTrueAndStoreEntry()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.IsTrue(cache.TryAdd("a", 1));
        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that adding an existing key returns <see langword="false" /> and leaves the stored value unchanged.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyExists_ShouldReturnFalseAndKeepExistingValue()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        Assert.IsFalse(cache.TryAdd("a", 2));
        Assert.IsTrue(cache.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.TryAdd(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a rejected add contributes nothing to the telemetry counters.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyExists_ShouldNotContributeToTelemetry()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        Assert.IsFalse(cache.TryAdd("a", 2));

        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);
    }
}

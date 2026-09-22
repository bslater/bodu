// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that an existing key is reported as present and a missing key as absent.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenProbed_ShouldReportPresence()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        Assert.IsTrue(cache.ContainsKey("a"));
        Assert.IsFalse(cache.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.ContainsKey(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a containment probe is pure: it contributes nothing to the telemetry counters.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenProbed_ShouldNotContributeToTelemetry()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        Assert.IsTrue(cache.ContainsKey("a"));
        Assert.IsFalse(cache.ContainsKey("missing"));

        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);
    }

    /// <summary>
    /// Verifies that a containment probe does not mark the entry accessed: the probed entry is still evicted ahead of
    /// a genuinely read peer.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenProbed_ShouldNotProtectEntryFromEviction()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 3);
        cache.Add("a", 1);
        Assert.IsTrue(cache.ContainsKey("a"));

        for (int i = 0; i < 10; i++)
            cache.Add($"filler{i}", i);
        cache.RunMaintenance();

        Assert.IsFalse(cache.ContainsKey("a"), "The pure probe must not have promoted 'a'; it should have flushed with the fillers.");
    }
}

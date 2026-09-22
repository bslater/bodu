// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that a new cache reports a count of zero and is empty.
    /// </summary>
    [TestMethod]
    public void Count_WhenEmpty_ShouldBeZero()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.AreEqual(0, cache.Count);
        Assert.IsTrue(cache.IsEmpty);
    }

    /// <summary>
    /// Verifies that the count tracks additions and explicit removals exactly.
    /// </summary>
    [TestMethod]
    public void Count_WhenMutated_ShouldTrackAddsAndRemoves()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 64);

        for (int i = 0; i < 20; i++)
            cache.Add(i, i);

        Assert.AreEqual(20, cache.Count);
        Assert.IsFalse(cache.IsEmpty);

        for (int i = 0; i < 10; i++)
            Assert.IsTrue(cache.TryRemove(i, out _));

        Assert.AreEqual(10, cache.Count);
    }

    /// <summary>
    /// Verifies that after single-threaded adds with interleaved maintenance the count never exceeds the capacity —
    /// the transient overshoot window requires concurrent in-flight writers.
    /// </summary>
    [TestMethod]
    public void Count_WhenSingleThreadedChurnConverges_ShouldStayWithinCapacity()
    {
        var cache = new ConcurrentLruCache<int, int>(capacity: 10);

        for (int i = 0; i < 200; i++)
        {
            cache.Add(i, i);
            cache.RunMaintenance();
            Assert.IsTrue(cache.Count <= 10, $"Count {cache.Count} exceeded capacity after add {i} with converged maintenance.");
        }
    }
}

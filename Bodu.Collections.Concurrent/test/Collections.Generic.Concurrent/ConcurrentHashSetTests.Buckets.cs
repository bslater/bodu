// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Buckets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the bucket-shortcut array doubles once the element count exceeds twice the bucket count, and
    /// doubles again on the next threshold — the load-factor policy of the split-ordered table.
    /// </summary>
    [TestMethod]
    public void BucketCount_WhenLoadFactorExceeded_ShouldDouble()
    {
        var set = new ConcurrentHashSet<int>(capacity: 8);
        Assert.AreEqual(8, set.BucketCount);

        for (int i = 0; i < 16; i++)
            set.Add(i);
        Assert.AreEqual(8, set.BucketCount, "At exactly the load-factor threshold the table must not yet grow.");

        set.Add(16);
        Assert.AreEqual(16, set.BucketCount, "Exceeding twice the bucket count must double the table.");

        for (int i = 17; i < 33; i++)
            set.Add(i);
        Assert.AreEqual(32, set.BucketCount, "Exceeding the next threshold must double the table again.");
    }

    /// <summary>
    /// Verifies that every element added before a growth cycle remains reachable afterwards — growth copies bucket
    /// shortcuts but never moves nodes, so lookups through both old and new shortcut arrays must keep succeeding.
    /// </summary>
    [TestMethod]
    public void BucketCount_WhenTableGrowsRepeatedly_ShouldKeepEveryElementReachable()
    {
        var set = new ConcurrentHashSet<int>(capacity: 8);
        const int count = 10_000;

        for (int i = 0; i < count; i++)
        {
            set.Add(i);

            // Probe an early and a recent key as the table doubles under the insert loop.
            Assert.IsTrue(set.Contains(0), $"Key 0 was lost after {i + 1} adds (buckets={set.BucketCount}).");
            Assert.IsTrue(set.Contains(i), $"Key {i} was not found immediately after its add.");
        }

        Assert.AreEqual(count, set.Count);
        for (int i = 0; i < count; i++)
            Assert.IsTrue(set.Contains(i), $"Key {i} was lost by growth.");
    }

    /// <summary>
    /// Verifies that a bucket whose shortcut is only created after several growth cycles still resolves lookups —
    /// lazy initialization must find the correct splice position via the parent sentinel chain.
    /// </summary>
    [TestMethod]
    public void BucketCount_WhenBucketInitializedLazilyAfterGrowth_ShouldResolveLookups()
    {
        var set = new ConcurrentHashSet<int>(capacity: 8);

        // Force several doublings with keys clustered in low buckets, then look up keys that map to buckets whose
        // shortcuts have never been touched in the grown table.
        for (int i = 0; i < 200; i++)
            set.Add(i * 256);

        for (int i = 0; i < 200; i++)
            Assert.IsTrue(set.Contains(i * 256), $"Key {i * 256} must be reachable through lazily created buckets.");

        for (int i = 0; i < 200; i++)
            Assert.IsFalse(set.Contains((i * 256) + 1), "A key that was never added must not be found.");
    }

    /// <summary>
    /// Verifies that when every element shares one hash code the entire population lands in a single equal-key run
    /// and the set still retains every key across growth cycles.
    /// </summary>
    [TestMethod]
    public void BucketCount_WhenAllKeysCollide_ShouldRetainEveryKeyAcrossGrowth()
    {
        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());

        for (int i = 0; i < 500; i++)
            Assert.IsTrue(set.Add(i), $"Add of colliding key {i} must succeed.");

        Assert.AreEqual(500, set.Count);
        for (int i = 0; i < 500; i++)
            Assert.IsTrue(set.Contains(i), $"Colliding key {i} was lost.");
    }
}

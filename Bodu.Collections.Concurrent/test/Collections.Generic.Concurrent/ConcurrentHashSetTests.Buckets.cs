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
    /// doubles again on the next threshold - the load-factor policy of the split-ordered table.
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
    /// Verifies that every element added before a growth cycle remains reachable afterwards - growth copies bucket
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
    /// Verifies that a bucket whose shortcut is only created after several growth cycles still resolves lookups -
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

    /// <summary>
    /// Verifies that when a bucket's sentinel is already linked into the list but not yet published to the shortcut
    /// array - a racing initializer between its two steps - the next operation on the bucket adopts that sentinel
    /// rather than linking a second one, publishes it, and finds its element through it.
    /// </summary>
    [TestMethod]
    public void InitializeBucket_WhenSentinelLinkedButUnpublished_ShouldAdoptAndPublishIt()
    {
        // With 32 buckets, 6 maps to bucket 6, whose parent bucket 2 is itself still uninitialized.
        var set = new ConcurrentHashSet<int>(capacity: 64);
        set.Add(1);
        Assert.AreEqual(32, set.BucketCount);
        Assert.IsNull(set.GetBucketShortcut(2));
        Assert.IsNull(set.GetBucketShortcut(6));
        object linked = set.LinkSentinelUnpublished(6);
        Assert.IsNotNull(set.GetBucketShortcut(2), "Linking initializes the parent bucket the way a racing initializer would.");
        Assert.IsNull(set.GetBucketShortcut(6), "Linking must not publish the sentinel.");
        Assert.AreSame(linked, set.LinkSentinelUnpublished(6), "A bucket has exactly one sentinel: linking again must yield the linked one.");

        Assert.IsTrue(set.Add(6));

        Assert.AreSame(linked, set.GetBucketShortcut(6), "The initializer must adopt and publish the linked sentinel.");
        Assert.IsTrue(set.Contains(6));
        AssertContainsExactly(set, 1, 6);
    }

    /// <summary>
    /// Verifies that when a competing initializer links and publishes the bucket's sentinel in the window between
    /// this initializer's traversal and its link compare-and-swap, the initializer loses the compare-and-swap,
    /// retries, adopts the winner's sentinel rather than replacing it, and the operation that triggered it completes
    /// through that sentinel.
    /// </summary>
    [TestMethod]
    public void InitializeBucket_WhenCompetingInitializerLinksSentinelBeforeCompareAndSwap_ShouldRetryAndAdoptWinner()
    {
        // With 32 buckets, 17 and 49 both map to bucket 17, whose parent bucket 1 already holds the element 1.
        var set = new ConcurrentHashSet<int>(capacity: 64);
        set.Add(1);
        Assert.AreEqual(32, set.BucketCount);
        object? winner = null;
        var hookInvocations = 0;
        set.InitializeBucketRaceHook = () =>
        {
            hookInvocations++;
            set.InitializeBucketRaceHook = null;

            // The competing initializer: a lookup of another element of bucket 17 links and publishes its sentinel.
            Assert.IsFalse(set.Contains(49));
            winner = set.GetBucketShortcut(17);
            Assert.IsNotNull(winner, "The competing initializer must have published the bucket's sentinel.");
        };

        bool added;
        try
        {
            added = set.Add(17);
        }
        finally
        {
            set.InitializeBucketRaceHook = null;
        }

        Assert.AreEqual(1, hookInvocations);
        Assert.IsTrue(added);
        Assert.AreSame(winner, set.GetBucketShortcut(17), "The loser must adopt the winner's sentinel rather than replace it.");
        Assert.IsTrue(set.Contains(17));
        AssertContainsExactly(set, 1, 17);
    }
}

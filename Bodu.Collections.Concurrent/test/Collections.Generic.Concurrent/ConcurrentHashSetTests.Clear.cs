// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> removes every element.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetHasElements_ShouldRemoveEveryElement()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.IsFalse(set.Contains(1));
    }

    /// <summary>
    /// Verifies that calling <see cref="ConcurrentHashSet{T}.Clear" /> on an empty set is a safe no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetIsEmpty_ShouldRemainEmpty()
    {
        var set = new ConcurrentHashSet<int>();

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that the set is fully usable after being cleared.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldAcceptNewElements()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Clear();

        Assert.IsTrue(set.Add(99));
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(99));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> empties a large set whose table has grown.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetIsLarge_ShouldRemoveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 5000));

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> retains the grown bucket count so a previously large
    /// set is not forced to immediately regrow on reuse.
    /// </summary>
    [TestMethod]
    public void Clear_WhenTableHasGrown_ShouldNotShrinkBelowGrownCapacity()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 20_000));
        int grownBucketCount = set.BucketCount;

        set.Clear();

        Assert.AreEqual(grownBucketCount, set.BucketCount, "Clear must not shrink the bucket table.");
    }

    /// <summary>
    /// Verifies that a set whose table has grown remains fully usable after <see cref="ConcurrentHashSet{T}.Clear" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenTableHasGrown_ShouldRemainFullyUsable()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 20_000));

        set.Clear();
        for (int i = 0; i < 5_000; i++)
            Assert.IsTrue(set.Add(i), $"Add of key {i} after Clear must succeed.");

        Assert.AreEqual(5_000, set.Count);
        for (int i = 0; i < 5_000; i++)
            Assert.IsTrue(set.Contains(i), $"Key {i} was lost after Clear and reuse.");
    }
}

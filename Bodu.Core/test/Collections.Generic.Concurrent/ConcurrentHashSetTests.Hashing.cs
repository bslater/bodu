// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Hashing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that distinct elements that all hash to the same bucket are each stored and retrievable.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenAllElementsCollide_ShouldStoreAndRetrieveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());

        for (var i = 0; i < 200; i++)
            Assert.IsTrue(set.Add(i));

        Assert.AreEqual(200, set.Count);
        for (var i = 0; i < 200; i++)
            Assert.IsTrue(set.Contains(i), $"Element {i} was not found in the colliding chain.");
    }

    /// <summary>
    /// Verifies that adding an element equal to one already present is rejected even when every element collides into
    /// the same bucket.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenAllElementsCollide_ShouldRejectDuplicates()
    {
        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());

        Assert.IsTrue(set.Add(42));
        Assert.IsFalse(set.Add(42));
        Assert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Verifies that removing several elements occupying different positions in a fully-colliding bucket chain leaves
    /// the remaining elements reachable.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenRemovingFromCollidingChain_ShouldKeepRemainingElementsReachable()
    {
        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());
        for (var i = 0; i < 10; i++)
            set.Add(i);

        // 9, 5, and 0 occupy distinct positions in the single colliding chain.
        Assert.IsTrue(set.Remove(9));
        Assert.IsTrue(set.Remove(5));
        Assert.IsTrue(set.Remove(0));

        AssertContainsExactly(set, 1, 2, 3, 4, 6, 7, 8);
    }

    /// <summary>
    /// Verifies that a fully-colliding comparer still allows the table to grow without losing or duplicating elements.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenAllElementsCollideAndTableGrows_ShouldPreserveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(capacity: 4, comparer: new ConstantHashComparer());

        for (var i = 0; i < 2000; i++)
            set.Add(i);

        Assert.AreEqual(2000, set.Count);
        Assert.HasCount(2000, set.ToArray());
    }

    /// <summary>
    /// Verifies that elements whose hash codes are extreme — including <see cref="int.MinValue" />, whose magnitude
    /// cannot be negated — are mapped to valid buckets and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenHashCodesAreExtreme_ShouldStoreAndRetrieveEveryElement()
    {
        var set = new ConcurrentHashSet<int>();
        int[] values = [int.MinValue, int.MaxValue, -1, 0, 1, int.MinValue + 1, int.MaxValue - 1];

        foreach (int value in values)
            Assert.IsTrue(set.Add(value));

        foreach (int value in values)
            Assert.IsTrue(set.Contains(value), $"Element {value} was not found.");

        Assert.AreEqual(values.Length, set.Count);
    }

    /// <summary>
    /// Verifies that a set composed entirely of negative-valued elements supports add, contains, and remove.
    /// </summary>
    [TestMethod]
    public void Hashing_WhenElementsAreNegative_ShouldSupportCoreOperations()
    {
        var set = new ConcurrentHashSet<int>();
        for (var i = -500; i < 0; i++)
            Assert.IsTrue(set.Add(i));

        Assert.AreEqual(500, set.Count);
        Assert.IsTrue(set.Contains(-250));
        Assert.IsTrue(set.Remove(-250));
        Assert.IsFalse(set.Contains(-250));
    }
}

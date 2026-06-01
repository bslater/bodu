// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.IntersectWith.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> keeps only the elements common to both
    /// collections.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenCollectionsOverlap_ShouldKeepOnlyCommonElements()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        set.IntersectWith([2, 3, 5]);

        AssertContainsExactly(set, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> with a disjoint collection empties the set.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenCollectionsAreDisjoint_ShouldEmptyTheSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.IntersectWith([7, 8, 9]);

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> with an empty collection empties the set.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsEmpty_ShouldEmptyTheSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.IntersectWith([]);

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> applied to the set itself leaves it unchanged.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsSameInstance_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.IntersectWith(set);

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> throws <see cref="ArgumentNullException" /> for
    /// a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IntersectWith(null!);
        });
    }
}

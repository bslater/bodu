// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ExceptWith.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> removes every element present in the other
    /// collection.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenCollectionsOverlap_ShouldRemoveCommonElements()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        set.ExceptWith([2, 4, 5]);

        AssertContainsExactly(set, 1, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> with a disjoint collection leaves the set
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenCollectionsAreDisjoint_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.ExceptWith([7, 8, 9]);

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> with an empty collection leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.ExceptWith(Array.Empty<int>());

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> applied to the set itself empties it.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsSameInstance_ShouldEmptyTheSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.ExceptWith(set);

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.ExceptWith(null!);
        });
    }
}

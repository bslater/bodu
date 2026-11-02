// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ToArray" /> returns an empty array for an empty set.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetIsEmpty_ShouldReturnEmptyArray()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.HasCount(0, set.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ToArray" /> returns every distinct element.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetHasElements_ShouldReturnEveryElement()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, set.ToArray());
    }

    /// <summary>
    /// Verifies that mutating the array returned by <see cref="ConcurrentHashSet{T}.ToArray" /> does not affect the
    /// set.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenReturnedArrayMutated_ShouldNotAffectSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        int[] array = set.ToArray();
        array[0] = 999;

        Assert.AreEqual(3, set.Count);
        Assert.IsFalse(set.Contains(999));
    }

    /// <summary>
    /// Verifies that the array returned by <see cref="ConcurrentHashSet{T}.ToArray" /> is a true snapshot, unaffected
    /// by later changes to the set.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetMutatedAfterCall_ShouldNotChangeReturnedArray()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        int[] array = set.ToArray();
        set.Add(4);
        set.Remove(1);

        Assert.HasCount(3, array);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ToArray" /> skips a node another thread has logically deleted but
    /// not yet unlinked - the marker installed, the unlink still pending - and keeps every other element, and that a
    /// later insert of the same element unlinks the marked node and lands.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenElementIsMarkedButNotUnlinked_ShouldOmitItAndKeepOthers()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);
        Assert.IsFalse(set.MarkRemovedWithoutUnlink(42), "An absent element has no node to mark.");
        Assert.IsTrue(set.MarkRemovedWithoutUnlink(2));

        int[] snapshot = set.ToArray();

        CollectionAssert.AreEquivalent(new[] { 1, 3 }, snapshot);
        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(2));
        Assert.IsTrue(set.Add(2), "A later insert must unlink the marked node and add a fresh element.");
        AssertContainsExactly(set, 1, 2, 3);
    }
}

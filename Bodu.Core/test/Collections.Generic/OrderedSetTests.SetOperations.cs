// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.SetOperations.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.ExceptWith(IEnumerable{T})" /> removes elements that are present
    /// in the source while preserving original order.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherHasItemsInCommon_ShouldRemoveThemInOriginalOrder()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3, 4]);

        sut.ExceptWith([2, 4, 99]);

        CollectionAssert.AreEqual(new[] { 1, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an empty source leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.ExceptWith([]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // ExceptWith
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.ExceptWith(IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.ExceptWith(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.ExceptWith(IEnumerable{T})" /> with the set itself clears the set.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsSelf_ShouldClearSet()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.ExceptWith(sut);

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.ExceptWith(IEnumerable{T})" /> on an empty set is a no-op.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenSetIsEmpty_ShouldRemainEmpty()
    {
        var sut = new OrderedSet<int>();

        sut.ExceptWith([1, 2, 3]);

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IntersectWith(IEnumerable{T})" /> keeps only the elements that
    /// are also in the source, preserving original insertion order.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherHasOverlap_ShouldRetainOnlyCommonInOriginalOrder()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3, 4]);

        sut.IntersectWith([4, 2, 5]);

        CollectionAssert.AreEqual(new[] { 2, 4 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an empty source clears the set.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsEmpty_ShouldClearSet()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.IntersectWith([]);

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // IntersectWith
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IntersectWith(IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.IntersectWith(null!);
        });
    }

    /// <summary>
    /// Verifies that intersecting the set with itself leaves it unchanged via the reference-identity fast path.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsSelf_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.IntersectWith(sut);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that intersecting an empty set with a non-empty source is a no-op.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenSetIsEmpty_ShouldRemainEmpty()
    {
        var sut = new OrderedSet<int>();

        sut.IntersectWith([1, 2, 3]);

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Comparer-aware set operations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that set operations honour the configured comparer when matching elements against the source.
    /// </summary>
    [TestMethod]
    public void SetOperations_WhenCustomComparerProvided_ShouldHonourComparer()
    {
        OrderedSet<string> sut = CreateSet(["Alpha", "Beta"], StringComparer.OrdinalIgnoreCase);

        sut.UnionWith(["ALPHA", "Gamma"]);

        Assert.AreEqual(3, sut.Count);
        Assert.IsTrue(sut.Contains("alpha"));
        Assert.IsTrue(sut.Contains("BETA"));
        Assert.IsTrue(sut.Contains("Gamma"));
    }

    /// <summary>
    /// Verifies that a source containing duplicates is deduplicated before applying symmetric difference.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherHasDuplicates_ShouldDeduplicateBeforeApplying()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        sut.SymmetricExceptWith([2, 2, 3, 3]);

        Assert.IsTrue(sut.Contains(1));
        Assert.IsFalse(sut.Contains(2));
        Assert.IsTrue(sut.Contains(3));
        Assert.AreEqual(2, sut.Count);
    }

    /// <summary>
    /// Verifies that an empty source leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.SymmetricExceptWith([]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // SymmetricExceptWith
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.SymmetricExceptWith(IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.SymmetricExceptWith(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.SymmetricExceptWith(IEnumerable{T})" /> with the set itself
    /// clears the set.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsSelf_ShouldClearSet()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.SymmetricExceptWith(sut);

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.SymmetricExceptWith(IEnumerable{T})" /> retains the elements
    /// exclusive to either side and removes the shared elements.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherOverlaps_ShouldKeepExclusiveElements()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.SymmetricExceptWith([2, 3, 4, 5]);

        Assert.AreEqual(3, sut.Count);
        Assert.IsTrue(sut.Contains(1));
        Assert.IsFalse(sut.Contains(2));
        Assert.IsFalse(sut.Contains(3));
        Assert.IsTrue(sut.Contains(4));
        Assert.IsTrue(sut.Contains(5));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.UnionWith(IEnumerable{T})" /> appends new items in source order
    /// after the existing contents.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherHasNewItems_ShouldAppendInSourceOrder()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        sut.UnionWith([2, 3, 4]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an empty source leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.UnionWith([]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }
    // --------------------------------------------------------
    // UnionWith
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.UnionWith(IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.UnionWith(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.UnionWith(IEnumerable{T})" /> with the set itself is a no-op
    /// because every element is already present.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsSelf_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.UnionWith(sut);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

}

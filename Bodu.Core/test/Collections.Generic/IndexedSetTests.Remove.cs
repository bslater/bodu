// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{
    // --------------------------------------------------------
    // Remove — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Remove(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<string> sut = CreateSet(["a"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Remove(null!);
        });
    }

    // --------------------------------------------------------
    // Remove — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing an absent item returns <see langword="false" /> and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsAbsent_ShouldReturnFalseAndLeaveSetUnchanged()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        var removed = sut.Remove(99);

        Assert.IsFalse(removed);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that removing a present item returns <see langword="true" /> and compacts the order.
    /// </summary>
    [TestMethod]
    [DataRow(1, new[] { 2, 3 })]
    [DataRow(2, new[] { 1, 3 })]
    [DataRow(3, new[] { 1, 2 })]
    public void Remove_WhenItemIsPresent_ShouldReturnTrueAndCompactOrder(int target, int[] expected)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        var removed = sut.Remove(target);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(expected, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // RemoveAt — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.RemoveAt(int)" /> rejects an out-of-range index.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void RemoveAt_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.RemoveAt(index);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.RemoveAt(int)" /> on an empty set throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenSetIsEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = new IndexedSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.RemoveAt(0);
        });
    }

    // --------------------------------------------------------
    // RemoveAt — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.RemoveAt(int)" /> removes the element at the given index and
    /// shifts later elements left.
    /// </summary>
    [TestMethod]
    [DataRow(0, new[] { 20, 30, 40 })]
    [DataRow(1, new[] { 10, 30, 40 })]
    [DataRow(2, new[] { 10, 20, 40 })]
    [DataRow(3, new[] { 10, 20, 30 })]
    public void RemoveAt_WhenIndexIsValid_ShouldRemoveElementAtPosition(int index, int[] expected)
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30, 40]);

        sut.RemoveAt(index);

        CollectionAssert.AreEqual(expected, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an item removed via <see cref="IndexedSet{T}.RemoveAt(int)" /> is no longer reported as
    /// present.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenItemRemoved_ShouldNoLongerBeContained()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut.RemoveAt(1);

        Assert.IsFalse(sut.Contains(2));
        Assert.AreEqual(-1, sut.IndexOf(2));
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Clear" /> empties the set.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllElements()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains(1));
    }

    /// <summary>
    /// Verifies that calling <see cref="IndexedSet{T}.Clear" /> on an already-empty set leaves it empty.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var sut = new IndexedSet<int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    // --------------------------------------------------------
    // Remove — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Remove(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        OrderedSetStorage<string> sut = CreateStorage(new[] { "a" });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Remove(null!);
        });
    }

    // --------------------------------------------------------
    // Remove — single-item behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing an absent item returns <see langword="false" /> and does not bump the version.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsAbsent_ShouldReturnFalseAndNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int versionBefore = sut._version;

        bool removed = sut.Remove(99);

        Assert.IsFalse(removed);
        Assert.AreEqual(versionBefore, sut._version);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that removing the first element shifts later elements left and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsFirst_ShouldShiftRemainingLeft()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.Remove(1));

        CollectionAssert.AreEqual(new[] { 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that removing an interior element shifts later elements left.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsInterior_ShouldShiftRemainingLeft()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3, 4 });

        Assert.IsTrue(sut.Remove(2));

        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that removing the last element trims the order with no shifts.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsLast_ShouldTrimOrder()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.Remove(3));

        CollectionAssert.AreEqual(new[] { 1, 2 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that removing an item also clears it from the hash table.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCalled_ShouldEvictFromHashTable()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.Remove(2);

        Assert.IsFalse(sut.Contains(2));
        Assert.AreEqual(-1, sut.IndexOf(2));
    }

    /// <summary>
    /// Verifies that re-adding a previously removed item appends it at the end of the order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemReAddedAfterRemoval_ShouldAppendAtEnd()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.Remove(2);
        sut.Add(2);

        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, Snapshot(sut));
    }

    // --------------------------------------------------------
    // RemoveAt — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an out-of-range index is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void RemoveAt_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.RemoveAt(index);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="OrderedSetStorage{T}.RemoveAt" /> on an empty storage with index zero
    /// throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenStorageIsEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.RemoveAt(0);
        });
    }

    // --------------------------------------------------------
    // RemoveAt — positional behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing at index zero shifts later elements left and decrements <see cref="OrderedSetStorage{T}.Count" />.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenIndexIsZero_ShouldShiftRemainingLeft()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.RemoveAt(0);

        CollectionAssert.AreEqual(new[] { 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that removing at the last index trims the order.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenIndexIsLast_ShouldTrimOrder()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.RemoveAt(2);

        CollectionAssert.AreEqual(new[] { 1, 2 }, Snapshot(sut));
    }

    // --------------------------------------------------------
    // RemoveWhere — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.RemoveWhere(System.Predicate{T})" /> rejects a <see langword="null" /> predicate.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenMatchIsNull_ShouldThrowArgumentNullException()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.RemoveWhere(null!);
        });
    }

    // --------------------------------------------------------
    // RemoveWhere — predicate behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.RemoveWhere(System.Predicate{T})" /> returns the number
    /// of removed elements and compacts the remainder in original order.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenSomeElementsMatch_ShouldRemoveMatchingAndKeepOrder()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3, 4, 5, 6 });

        int removed = sut.RemoveWhere(x => x % 2 == 0);

        Assert.AreEqual(3, removed);
        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that when no element matches, <see cref="OrderedSetStorage{T}.RemoveWhere(System.Predicate{T})" />
    /// returns zero and does not bump the version.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenNothingMatches_ShouldReturnZeroAndNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int versionBefore = sut._version;

        int removed = sut.RemoveWhere(_ => false);

        Assert.AreEqual(0, removed);
        Assert.AreEqual(versionBefore, sut._version);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that when every element matches, the storage is emptied.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenAllMatch_ShouldEmptyStorage()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        int removed = sut.RemoveWhere(_ => true);

        Assert.AreEqual(3, removed);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that after <see cref="OrderedSetStorage{T}.RemoveWhere(System.Predicate{T})" />, surviving
    /// elements are still findable through the hash table.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenSomeElementsMatch_ShouldRehashSurvivors()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3, 4 });

        sut.RemoveWhere(x => x == 2);

        Assert.IsTrue(sut.Contains(1));
        Assert.IsFalse(sut.Contains(2));
        Assert.IsTrue(sut.Contains(3));
        Assert.IsTrue(sut.Contains(4));
        Assert.AreEqual(0, sut.IndexOf(1));
        Assert.AreEqual(1, sut.IndexOf(3));
        Assert.AreEqual(2, sut.IndexOf(4));
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Clear" /> empties the storage and bumps the version.
    /// </summary>
    [TestMethod]
    public void Clear_WhenStorageHasItems_ShouldEmptyStorage()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int versionBefore = sut._version;

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreNotEqual(versionBefore, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Clear" /> on an already-empty storage is a no-op and
    /// does not bump the version.
    /// </summary>
    [TestMethod]
    public void Clear_WhenStorageIsEmpty_ShouldBeNoOp()
    {
        var sut = new OrderedSetStorage<int>(0, null);
        int versionBefore = sut._version;

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(versionBefore, sut._version);
    }

    /// <summary>
    /// Verifies that items re-added after <see cref="OrderedSetStorage{T}.Clear" /> are tracked correctly.
    /// </summary>
    [TestMethod]
    public void Clear_WhenItemsReAddedAfterClear_ShouldTrackNewItems()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.Clear();
        sut.Add(99);

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(99, sut.GetAt(0));
        Assert.IsTrue(sut.Contains(99));
    }
}

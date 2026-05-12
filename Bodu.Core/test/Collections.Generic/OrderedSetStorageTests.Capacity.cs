// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    // --------------------------------------------------------
    // Count
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Count" /> is zero immediately after construction.
    /// </summary>
    [TestMethod]
    public void Count_WhenStorageIsNewlyConstructed_ShouldBeZero()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Count" /> reflects the number of currently-stored
    /// elements across additions and removals.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAddedAndRemoved_ShouldReflectCurrentSize()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        sut.Add(1);
        sut.Add(2);
        sut.Add(3);
        sut.Remove(2);

        Assert.AreEqual(2, sut.Count);
    }

    // --------------------------------------------------------
    // EnsureCapacity — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a negative capacity is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.EnsureCapacity(capacity);
        });
    }

    // --------------------------------------------------------
    // EnsureCapacity — growth behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.EnsureCapacity(int)" /> grows the element array when the
    /// requested capacity exceeds the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityExceedsCurrent_ShouldGrow()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        int newCapacity = sut.EnsureCapacity(128);

        Assert.IsTrue(newCapacity >= 128);
        Assert.IsTrue(sut.Capacity >= 128);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.EnsureCapacity(int)" /> is a no-op when the requested
    /// capacity does not exceed the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityDoesNotExceedCurrent_ShouldNotShrink()
    {
        var sut = new OrderedSetStorage<int>(16, null);

        int newCapacity = sut.EnsureCapacity(4);

        Assert.AreEqual(16, sut.Capacity);
        Assert.AreEqual(16, newCapacity);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.EnsureCapacity(int)" /> preserves existing elements.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrown_ShouldPreserveContents()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });

        sut.EnsureCapacity(128);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
        Assert.IsTrue(sut.Capacity >= 128);
    }

    // --------------------------------------------------------
    // TrimExcess
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.TrimExcess" /> on an empty storage resets internal
    /// arrays to length zero.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenStorageIsEmpty_ShouldResetCapacityToZero()
    {
        var sut = new OrderedSetStorage<int>(16, null);

        sut.TrimExcess();

        Assert.AreEqual(0, sut.Capacity);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.TrimExcess" /> on a storage where capacity matches count
    /// is a no-op.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityEqualsCount_ShouldBeNoOp()
    {
        var sut = new OrderedSetStorage<int>(0, null);
        sut.Add(1);
        sut.Add(2);
        sut.TrimExcess();
        int capacityBefore = sut.Capacity;

        sut.TrimExcess();

        Assert.AreEqual(capacityBefore, sut.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 2 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.TrimExcess" /> on a partially-filled storage shrinks
    /// capacity down to <see cref="OrderedSetStorage{T}.Count" /> and preserves contents.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityExceedsCount_ShouldShrinkAndPreserveContents()
    {
        var sut = new OrderedSetStorage<int>(128, null);
        sut.Add(1);
        sut.Add(2);
        sut.Add(3);

        sut.TrimExcess();

        Assert.AreEqual(3, sut.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that elements remain findable through the hash table after <see cref="OrderedSetStorage{T}.TrimExcess" />.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalled_ShouldKeepItemsFindable()
    {
        var sut = new OrderedSetStorage<int>(128, null);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        sut.TrimExcess();

        Assert.IsTrue(sut.Contains(10));
        Assert.AreEqual(1, sut.IndexOf(20));
        Assert.AreEqual(2, sut.IndexOf(30));
    }
}

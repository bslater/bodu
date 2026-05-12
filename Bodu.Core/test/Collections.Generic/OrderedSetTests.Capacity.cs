// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    // --------------------------------------------------------
    // EnsureCapacity
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.EnsureCapacity(int)" /> rejects a negative capacity.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.EnsureCapacity(capacity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.EnsureCapacity(int)" /> grows to at least the requested
    /// capacity and reports it.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenLargerCapacityRequested_ShouldGrow()
    {
        var sut = new OrderedSet<int>();

        int reported = sut.EnsureCapacity(128);

        Assert.IsTrue(reported >= 128);
        Assert.IsTrue(sut.Capacity >= 128);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.EnsureCapacity(int)" /> is a no-op when the requested capacity
    /// does not exceed the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenSmallerCapacityRequested_ShouldNotShrink()
    {
        var sut = new OrderedSet<int>(64);
        int capacityBefore = sut.Capacity;

        int reported = sut.EnsureCapacity(4);

        Assert.AreEqual(capacityBefore, sut.Capacity);
        Assert.AreEqual(capacityBefore, reported);
    }

    /// <summary>
    /// Verifies that growth preserves the existing contents.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrown_ShouldPreserveContents()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        sut.EnsureCapacity(128);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // TrimExcess
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.TrimExcess" /> on an empty set releases the backing arrays.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenSetIsEmpty_ShouldResetCapacityToZero()
    {
        var sut = new OrderedSet<int>(16);

        sut.TrimExcess();

        Assert.AreEqual(0, sut.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.TrimExcess" /> on a partially-filled set shrinks capacity to
    /// <see cref="OrderedSet{T}.Count" /> and preserves contents.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityExceedsCount_ShouldShrinkAndPreserveContents()
    {
        var sut = new OrderedSet<int>(128);
        sut.Add(1);
        sut.Add(2);
        sut.Add(3);

        sut.TrimExcess();

        Assert.AreEqual(3, sut.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that items remain locatable after <see cref="OrderedSet{T}.TrimExcess" />.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalled_ShouldKeepItemsLocatable()
    {
        var sut = new OrderedSet<int>(128);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        sut.TrimExcess();

        Assert.IsTrue(sut.Contains(20));
        Assert.AreEqual(2, sut.IndexOf(30));
    }
}

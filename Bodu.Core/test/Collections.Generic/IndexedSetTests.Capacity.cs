// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{
    // --------------------------------------------------------
    // EnsureCapacity
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.EnsureCapacity(int)" /> rejects a negative capacity.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var sut = new IndexedSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.EnsureCapacity(capacity);
        });
    }

    /// <summary>
    /// Verifies that growth preserves the existing contents.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrown_ShouldPreserveContents()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut.EnsureCapacity(128);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.EnsureCapacity(int)" /> grows to at least the requested capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenLargerCapacityRequested_ShouldGrow()
    {
        var sut = new IndexedSet<int>();

        var reported = sut.EnsureCapacity(128);

        Assert.IsGreaterThanOrEqualTo(128, reported);
        Assert.IsGreaterThanOrEqualTo(128, sut.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.EnsureCapacity(int)" /> does not shrink when the requested
    /// capacity is below the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenSmallerCapacityRequested_ShouldNotShrink()
    {
        var sut = new IndexedSet<int>(64);
        var capacityBefore = sut.Capacity;

        var reported = sut.EnsureCapacity(4);

        Assert.AreEqual(capacityBefore, sut.Capacity);
        Assert.AreEqual(capacityBefore, reported);
    }

    /// <summary>
    /// Verifies that items remain locatable after <see cref="IndexedSet{T}.TrimExcess" />.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalled_ShouldKeepItemsLocatable()
    {
        var sut = new IndexedSet<int>(128);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        sut.TrimExcess();

        Assert.IsTrue(sut.Contains(20));
        Assert.AreEqual(2, sut.IndexOf(30));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.TrimExcess" /> shrinks capacity to <see cref="IndexedSet{T}.Count" />
    /// and preserves contents.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityExceedsCount_ShouldShrinkAndPreserveContents()
    {
        var sut = new IndexedSet<int>(128);
        sut.Add(1);
        sut.Add(2);
        sut.Add(3);

        sut.TrimExcess();

        Assert.AreEqual(3, sut.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // TrimExcess
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.TrimExcess" /> on an empty set releases the backing arrays.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenSetIsEmpty_ShouldResetCapacityToZero()
    {
        var sut = new IndexedSet<int>(16);

        sut.TrimExcess();

        Assert.AreEqual(0, sut.Capacity);
    }

}

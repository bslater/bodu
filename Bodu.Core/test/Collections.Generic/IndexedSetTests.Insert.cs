// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Insert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that the <see cref="IndexedSet{T}.Insert(int, T)" /> entry point implements the
    /// <see cref="System.Collections.Generic.IList{T}.Insert(int, T)" /> contract correctly when invoked via
    /// the typed interface.
    /// </summary>
    [TestMethod]
    public void IListInsert_WhenCalledThroughInterface_ShouldDelegateToInsert()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);
        System.Collections.Generic.IList<int> typed = sut;

        typed.Insert(0, 99);

        CollectionAssert.AreEqual(new[] { 99, 10, 20, 30 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Insert(int, T)" /> rejects an out-of-range index.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MinValue)]
    public void Insert_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Insert(index, 99);
        });
    }

    // --------------------------------------------------------
    // Insert — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Insert(int, T)" /> places the item at the requested index and
    /// shifts later elements rightwards.
    /// </summary>
    [TestMethod]
    public void Insert_WhenItemAndIndexAreValid_ShouldPlaceItemAtPosition()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);

        sut.Insert(1, 99);

        CollectionAssert.AreEqual(new[] { 10, 99, 20, 30 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Insert(int, T)" /> throws <see cref="ArgumentException" /> when
    /// the item is already in the set.
    /// </summary>
    [TestMethod]
    public void Insert_WhenItemIsDuplicate_ShouldThrowExactly()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Insert(0, 2);
        });
    }

    // --------------------------------------------------------
    // Insert — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Insert(int, T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Insert_WhenItemIsNull_ShouldThrowExactly()
    {
        IndexedSet<string> sut = CreateSet(["a", "b"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Insert(0, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.TryInsert(int, T)" /> rejects an out-of-range index.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void TryInsert_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.TryInsert(index, 99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.TryInsert(int, T)" /> appends, prepends, or inserts the value
    /// at the requested position depending on the index.
    /// </summary>
    [TestMethod]
    [DataRow(0, new[] { 99, 1, 2, 3 })]
    [DataRow(1, new[] { 1, 99, 2, 3 })]
    [DataRow(2, new[] { 1, 2, 99, 3 })]
    [DataRow(3, new[] { 1, 2, 3, 99 })]
    public void TryInsert_WhenIndexIsValid_ShouldPlaceItemAtPosition(int index, int[] expected)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsTrue(sut.TryInsert(index, 99));

        CollectionAssert.AreEqual(expected, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an inserted item becomes locatable through <see cref="IndexedSet{T}.IndexOf(T)" />.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemInserted_ShouldUpdateIndexOf()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut.TryInsert(1, 99);

        Assert.AreEqual(1, sut.IndexOf(99));
        Assert.AreEqual(2, sut.IndexOf(2));
        Assert.AreEqual(3, sut.IndexOf(3));
    }

    // --------------------------------------------------------
    // TryInsert — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a duplicate insertion returns <see langword="false" /> and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemIsDuplicate_ShouldReturnFalseAndPreserveOrder()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        var inserted = sut.TryInsert(0, 2);

        Assert.IsFalse(inserted);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }
    // --------------------------------------------------------
    // TryInsert — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.TryInsert(int, T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemIsNull_ShouldThrowExactly()
    {
        IndexedSet<string> sut = CreateSet(["a", "b"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.TryInsert(0, null!);
        });
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{
    // --------------------------------------------------------
    // Indexer get — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that getting the indexer with an out-of-range index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void Indexer_Get_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut[index];
        });
    }

    /// <summary>
    /// Verifies that the indexer returns elements in insertion order.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenSetPopulated_ShouldReturnElementInInsertionOrder()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);

        Assert.AreEqual(10, sut[0]);
        Assert.AreEqual(20, sut[1]);
        Assert.AreEqual(30, sut[2]);
    }

    // --------------------------------------------------------
    // Indexer set — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that assigning to the indexer with an out-of-range index throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MaxValue)]
    public void Indexer_Set_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut[index] = 99;
        });
    }

    /// <summary>
    /// Verifies that assigning a value that already exists at a different index throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenValueExistsAtAnotherIndex_ShouldThrowArgumentException()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut[0] = 3;
        });
    }

    /// <summary>
    /// Verifies that assigning a value equal to the one already at that index is a no-op.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenValueIsAlreadyAtIndex_ShouldBeNoOp()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut[1] = 2;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
        Assert.IsTrue(sut.Contains(2));
    }

    // --------------------------------------------------------
    // Indexer set — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that assigning a new value through the indexer replaces the element at that index and updates
    /// the hash table.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenValueIsNew_ShouldReplaceAtPositionAndUpdateLookup()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut[1] = 99;

        CollectionAssert.AreEqual(new[] { 1, 99, 3 }, SnapshotByIndexer(sut));
        Assert.IsTrue(sut.Contains(99));
        Assert.IsFalse(sut.Contains(2));
        Assert.AreEqual(1, sut.IndexOf(99));
    }

    /// <summary>
    /// Verifies that assigning a <see langword="null" /> reference through the indexer throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<string> sut = CreateSet(["a", "b"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut[0] = null!;
        });
    }

}

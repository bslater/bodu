// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.Indexer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that the indexer throws when given an index at or above <see cref="GetCount(TCollection)"/>.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexAtOrAboveCount_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = GetAt(collection, 1);
        });
    }

    /// <summary>
    /// Verifies that the indexer throws when given a negative index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = GetAt(collection, -1);
        });
    }
    /// <summary>
    /// Verifies that the indexer returns elements in head-to-tail order for sequential tail-side adds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenItemsAddedAtTail_ShouldReturnInOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 10);
        AddToTail(collection, 20);
        AddToTail(collection, 30);

        Assert.AreEqual(10, GetAt(collection, 0));
        Assert.AreEqual(20, GetAt(collection, 1));
        Assert.AreEqual(30, GetAt(collection, 2));
    }

    /// <summary>
    /// Verifies that the indexer returns the correct element when the storage has wrapped around the
    /// internal array boundary, across a range of capacities.
    /// </summary>
    /// <param name="capacity">The collection capacity. Drives where the wrap boundary falls.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(16)]
    public void Indexer_WhenStorageWrapped_ShouldReturnLogicalOrder(int capacity)
    {
        // Fill to capacity, drain one, refill — this places the head past zero and the tail wrapped to slot zero.
        TCollection collection = CreateCollection(capacity);
        for (var i = 0; i < capacity; i++)
            AddToTail(collection, i);

        _ = RemoveFromHead(collection);
        AddToTail(collection, capacity); // wraps into slot 0

        // Logical order is now [1, 2, ..., capacity]
        for (var i = 0; i < capacity; i++)
            Assert.AreEqual(i + 1, GetAt(collection, i));
    }

}

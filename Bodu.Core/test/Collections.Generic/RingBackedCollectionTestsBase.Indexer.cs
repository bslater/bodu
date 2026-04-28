// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that the indexer returns elements in head-to-tail order for sequential tail-side adds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenItemsAddedAtTail_ShouldReturnInOrder()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 10);
        AddToTail(collection, 20);
        AddToTail(collection, 30);

        Assert.AreEqual(10, GetAt(collection, 0));
        Assert.AreEqual(20, GetAt(collection, 1));
        Assert.AreEqual(30, GetAt(collection, 2));
    }

    /// <summary>
    /// Verifies that the indexer returns the correct element when the storage has wrapped around the
    /// internal array boundary.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenStorageWrapped_ShouldReturnLogicalOrder()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        _ = RemoveFromHead(collection);
        AddToTail(collection, 4); // wraps

        Assert.AreEqual(2, GetAt(collection, 0));
        Assert.AreEqual(3, GetAt(collection, 1));
        Assert.AreEqual(4, GetAt(collection, 2));
    }

    /// <summary>
    /// Verifies that the indexer throws when given a negative index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = GetAt(collection, -1);
        });
    }

    /// <summary>
    /// Verifies that the indexer throws when given an index at or above <see cref="GetCount(TCollection)"/>.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexAtOrAboveCount_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = GetAt(collection, 1);
        });
    }
}

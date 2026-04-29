// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that the indexer returns elements in head-to-tail order after tail-side adds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAddedAtTail_ShouldReturnInOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(10);
        deque.AddLast(20);
        deque.AddLast(30);

        Assert.AreEqual(10, deque[0]);
        Assert.AreEqual(20, deque[1]);
        Assert.AreEqual(30, deque[2]);
    }

    /// <summary>
    /// Verifies that the indexer returns elements in head-to-tail order after head-side adds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAddedAtHead_ShouldReturnInReverseInsertionOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddFirst(10);
        deque.AddFirst(20);
        deque.AddFirst(30);

        Assert.AreEqual(30, deque[0]);
        Assert.AreEqual(20, deque[1]);
        Assert.AreEqual(10, deque[2]);
    }

    /// <summary>
    /// Verifies that the indexer returns the correct element when the storage has wrapped around.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenStorageWrapped_ShouldReturnCorrectElement()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst(); // head moves
        deque.AddLast(4); // wraps

        Assert.AreEqual(2, deque[0]);
        Assert.AreEqual(3, deque[1]);
        Assert.AreEqual(4, deque[2]);
    }

    /// <summary>
    /// Verifies that the indexer throws when given a negative index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = deque[-1];
        });
    }

    /// <summary>
    /// Verifies that the indexer throws when the index is at or above <see cref="ArrayDeque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexAtOrAboveCount_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = deque[1];
        });
    }
}

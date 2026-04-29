// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that the indexer returns elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAddedAtTail_ShouldReturnInOrder()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(10);
        deque.AddLast(20);
        deque.AddLast(30);
        Assert.AreEqual(10, deque[0]);
        Assert.AreEqual(20, deque[1]);
        Assert.AreEqual(30, deque[2]);
    }

    /// <summary>
    /// Verifies that the indexer throws on negative index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = deque[-1];
        });
    }

    /// <summary>
    /// Verifies that the indexer throws when the index is at or above <see cref="Deque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexAtOrAboveCount_ShouldThrowExactly()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = deque[1];
        });
    }
}

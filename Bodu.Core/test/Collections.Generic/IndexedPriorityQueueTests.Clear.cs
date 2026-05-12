// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Clear" /> empties the queue and resets <c>Count</c>.
    /// </summary>
    [TestMethod]
    public void Clear_WhenQueueIsNonEmpty_ShouldRemoveAllElements()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);

        queue.Clear();

        Assert.AreEqual(0, queue.Count);
        Assert.IsFalse(queue.Contains("a"));
        Assert.IsFalse(queue.Contains("b"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Clear" /> on an empty queue is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenQueueIsEmpty_ShouldRemainEmpty()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Clear();

        Assert.AreEqual(0, queue.Count);
    }

    /// <summary>
    /// Verifies that the queue can be reused after <see cref="IndexedPriorityQueue{TElement, TPriority}.Clear" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByEnqueue_ShouldAcceptNewElements()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Clear();
        queue.Enqueue("a", 99);

        Assert.AreEqual(1, queue.Count);
        Assert.AreEqual(99, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Clear" /> preserves the backing capacity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvoked_ShouldPreserveCapacity()
    {
        var queue = new IndexedPriorityQueue<string, int>(64);
        queue.Enqueue("a", 1);

        var capacityBefore = queue.Capacity;
        queue.Clear();

        Assert.AreEqual(capacityBefore, queue.Capacity);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Dequeue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Dequeue" /> removes the element with the smallest priority.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenQueueIsNonEmpty_ShouldReturnSmallestPriorityElement()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 30);
        queue.Enqueue("b", 10);
        queue.Enqueue("c", 20);

        var head = queue.Dequeue();

        Assert.AreEqual("b", head.Key);
        Assert.AreEqual(10, head.Value);
        Assert.AreEqual(2, queue.Count);
        Assert.IsFalse(queue.Contains("b"));
    }

    /// <summary>
    /// Verifies that draining the queue yields elements in non-decreasing priority order.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenDrainingMultipleElements_ShouldYieldPriorityOrder()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        int[] priorities = { 50, 10, 40, 20, 30, 5, 70, 60 };
        for (int i = 0; i < priorities.Length; i++)
            queue.Enqueue(i, priorities[i]);

        var drained = DrainAll(queue);

        AssertNonDecreasing(drained);
        Assert.AreEqual(priorities.Length, drained.Length);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Dequeue" /> on an empty queue throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenQueueIsEmpty_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = queue.Dequeue();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryDequeue" /> returns <see langword="false" /> on an empty queue and leaves outputs at default.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenQueueIsEmpty_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsFalse(queue.TryDequeue(out string? element, out int priority));
        Assert.IsNull(element);
        Assert.AreEqual(0, priority);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryDequeue" /> returns the head pair when the queue is non-empty.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenQueueIsNonEmpty_ShouldReturnHeadPair()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 5);
        queue.Enqueue("b", 1);

        Assert.IsTrue(queue.TryDequeue(out string? element, out int priority));
        Assert.AreEqual("b", element);
        Assert.AreEqual(1, priority);
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>
    /// Verifies that with a reverse priority comparer the queue behaves as a max-heap.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenComparerIsReversed_ShouldBehaveAsMaxHeap()
    {
        IComparer<int> reverse = Comparer<int>.Create((a, b) => b.CompareTo(a));
        var queue = new IndexedPriorityQueue<string, int>(0, reverse);
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 30);
        queue.Enqueue("c", 20);

        Assert.AreEqual("b", queue.Dequeue().Key);
        Assert.AreEqual("c", queue.Dequeue().Key);
        Assert.AreEqual("a", queue.Dequeue().Key);
    }
}

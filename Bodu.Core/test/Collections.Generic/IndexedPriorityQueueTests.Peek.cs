// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Peek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Peek" /> returns the smallest-priority pair without removing it.
    /// </summary>
    [TestMethod]
    public void Peek_WhenQueueIsNonEmpty_ShouldReturnHeadWithoutRemoving()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 30);
        queue.Enqueue("b", 10);

        var head = queue.Peek();

        Assert.AreEqual("b", head.Key);
        Assert.AreEqual(10, head.Value);
        Assert.AreEqual(2, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Peek" /> on an empty queue throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Peek_WhenQueueIsEmpty_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = queue.Peek();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryPeek" /> returns <see langword="false" /> on an empty queue.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenQueueIsEmpty_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsFalse(queue.TryPeek(out string? element, out int priority));
        Assert.IsNull(element);
        Assert.AreEqual(0, priority);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryPeek" /> returns the head pair when the queue is non-empty.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenQueueIsNonEmpty_ShouldReturnHead()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 50);
        queue.Enqueue("b", 5);

        Assert.IsTrue(queue.TryPeek(out string? element, out int priority));
        Assert.AreEqual("b", element);
        Assert.AreEqual(5, priority);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryPeek" /> returns <see langword="false" />
    /// once every element has been drained from the queue.
    /// </summary>
    [TestMethod]
    public void TryPeek_AfterDrainingQueue_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        _ = queue.Dequeue();
        _ = queue.Dequeue();

        Assert.IsFalse(queue.TryPeek(out string? element, out int priority));
        Assert.IsNull(element);
        Assert.AreEqual(0, priority);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Peek" /> does not mutate the queue,
    /// allowing repeated peeks to return the same head pair.
    /// </summary>
    [TestMethod]
    public void Peek_WhenInvokedRepeatedly_ShouldReturnSameHead()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 5);

        var first = queue.Peek();
        var second = queue.Peek();

        Assert.AreEqual(first.Key, second.Key);
        Assert.AreEqual(first.Value, second.Value);
        Assert.AreEqual(2, queue.Count);
    }
}

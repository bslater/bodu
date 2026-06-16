// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Enqueue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that the element comparer (e.g., case-insensitive) drives duplicate detection.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenElementComparerIsCaseInsensitive_ShouldRejectCaseVariantAsDuplicate()
    {
        var queue = new IndexedPriorityQueue<string, int>(0, null, StringComparer.OrdinalIgnoreCase);
        queue.Enqueue("a", 1);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            queue.Enqueue("A", 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Enqueue" /> with a duplicate element throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenElementIsDuplicate_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            queue.Enqueue("a", 2);
        });
    }
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Enqueue" /> adds an element and increments <c>Count</c>.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenElementIsNew_ShouldAddAndIncrementCount()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.AreEqual(1, queue.Count);
        Assert.IsTrue(queue.Contains("a"));
    }

    /// <summary>
    /// Verifies that enqueuing a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            queue.Enqueue(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that successive enqueues grow the backing array beyond the initial capacity.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenExceedingInitialCapacity_ShouldGrowBacking()
    {
        var queue = new IndexedPriorityQueue<int, int>(2);

        for (int i = 0; i < 100; i++)
            queue.Enqueue(i, i);

        Assert.AreEqual(100, queue.Count);
        Assert.IsGreaterThanOrEqualTo(100, queue.Capacity);
    }

    /// <summary>
    /// Verifies that enqueuing into a queue created with zero initial capacity grows the backing storage from empty.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenInitialCapacityIsZero_ShouldGrowFromEmpty()
    {
        var queue = new IndexedPriorityQueue<int, int>();

        queue.Enqueue(1, 1);

        Assert.AreEqual(1, queue.Count);
        Assert.IsGreaterThanOrEqualTo(1, queue.Capacity);
    }

    /// <summary>
    /// Verifies that an enqueue that becomes the new minimum is the next dequeued.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenNewElementHasSmallestPriority_ShouldBecomeHead()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 50);
        queue.Enqueue("b", 100);
        queue.Enqueue("c", 1);

        Assert.AreEqual("c", queue.Peek().Key);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnqueueOrUpdate" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void EnqueueOrUpdate_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.EnqueueOrUpdate(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that a failed <see cref="IndexedPriorityQueue{TElement, TPriority}.TryEnqueue" /> does not overwrite the priority of the existing element.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenElementIsDuplicate_ShouldPreserveExistingPriority()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        bool added = queue.TryEnqueue("a", 99);

        Assert.IsFalse(added);
        Assert.AreEqual(1, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryEnqueue" /> returns <see langword="false" /> for a duplicate element and leaves the queue unchanged.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenElementIsDuplicate_ShouldReturnFalseAndPreserveCount()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.IsFalse(queue.TryEnqueue("a", 99));
        Assert.AreEqual(1, queue.Count);
        Assert.AreEqual(1, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryEnqueue" /> returns <see langword="true" /> for a new element.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenElementIsNew_ShouldReturnTrue()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsTrue(queue.TryEnqueue("a", 1));
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryEnqueue" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.TryEnqueue(null!, 1);
        });
    }

}

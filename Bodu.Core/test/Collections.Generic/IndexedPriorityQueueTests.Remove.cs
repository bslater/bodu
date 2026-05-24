// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that removing every element in arbitrary order leaves the queue empty and consistent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenAllElementsRemoved_ShouldEmptyQueue()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 20; i++)
            queue.Enqueue(i, (i * 17) % 53);

        foreach (var key in new[] { 7, 0, 19, 12, 3, 15, 1, 8, 11, 4, 18, 2, 16, 6, 13, 5, 10, 14, 9, 17 })
            Assert.IsTrue(queue.Remove(key));

        Assert.AreEqual(0, queue.Count);
        Assert.IsFalse(queue.TryPeek(out _, out _));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Remove" /> uses the configured
    /// element comparer when matching case-insensitive variants.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementComparerIsCaseInsensitive_ShouldMatchCaseVariant()
    {
        var queue = new IndexedPriorityQueue<string, int>(0, null, StringComparer.OrdinalIgnoreCase);
        queue.Enqueue("alpha", 1);

        Assert.IsTrue(queue.Remove("ALPHA"));
        Assert.AreEqual(0, queue.Count);
    }
    /// <summary>
    /// Verifies that removing the head element preserves the heap invariant for the remainder.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementIsHead_ShouldDropHeadAndPreserveInvariant()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        queue.Enqueue("c", 3);
        queue.Enqueue("d", 4);

        Assert.IsTrue(queue.Remove("a"));
        Assert.AreEqual(3, queue.Count);
        Assert.AreEqual("b", queue.Peek().Key);
    }

    /// <summary>
    /// Verifies that removing the last (trailing) heap-storage element does not corrupt the queue.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementIsLastInStorage_ShouldRemoveCleanly()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 10; i++)
            queue.Enqueue(i, i);

        // The last storage slot's element is implementation-defined; remove every element by
        // dequeue order to confirm no corruption.
        Assert.IsTrue(queue.Remove(9));
        Assert.IsFalse(queue.Contains(9));

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.HasCount(9, drained);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Remove" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.Remove(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Remove" /> returns <see langword="false" /> for a missing element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementMissing_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        Assert.IsFalse(queue.Remove("missing"));
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>
    /// Verifies that an element re-enqueued after removal is treated as new.
    /// </summary>
    [TestMethod]
    public void Remove_WhenFollowedByEnqueue_ShouldAcceptElementAsNew()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Remove("a");
        queue.Enqueue("a", 99);

        Assert.AreEqual(99, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that the heap invariant survives a sequence of removals that triggers both sift-up and sift-down repairs.
    /// </summary>
    [TestMethod]
    public void Remove_WhenManyMixedElements_ShouldMaintainHeapInvariant()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 50; i++)
            queue.Enqueue(i, (i * 13) % 97);

        // Remove a scattered subset.
        foreach (var key in new[] { 0, 5, 10, 15, 25, 35, 49 })
            Assert.IsTrue(queue.Remove(key));

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.HasCount(50 - 7, drained);
    }

    /// <summary>
    /// Verifies that removing an internal element whose replacement must sift downward repairs the heap.
    /// </summary>
    [TestMethod]
    public void Remove_WhenReplacementRequiresSiftDown_ShouldRepairHeap()
    {
        // After inserts the trailing storage slot has a large priority; removing an internal node
        // forces that node to sift down past smaller children.
        var queue = new IndexedPriorityQueue<int, int>();
        queue.Enqueue(1, 1);
        queue.Enqueue(2, 5);
        queue.Enqueue(3, 10);
        queue.Enqueue(4, 7);
        queue.Enqueue(5, 8);
        queue.Enqueue(6, 999);

        Assert.IsTrue(queue.Remove(2));

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.HasCount(5, drained);
    }

    /// <summary>
    /// Verifies that removing an internal element whose replacement must sift upward repairs the heap.
    /// </summary>
    [TestMethod]
    public void Remove_WhenReplacementRequiresSiftUp_ShouldRepairHeap()
    {
        // The insertion order below produces a heap in which the tail slot's priority (15) is smaller
        // than the priority being removed (100), forcing the trailing replacement to sift upward.
        var queue = new IndexedPriorityQueue<int, int>();
        queue.Enqueue(1, 10);
        queue.Enqueue(2, 50);
        queue.Enqueue(3, 20);
        queue.Enqueue(4, 100);
        queue.Enqueue(5, 200);
        queue.Enqueue(6, 15);

        Assert.IsTrue(queue.Remove(4));

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.HasCount(5, drained);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 5, 6 }, drained.Select(p => p.Key).ToArray());
    }

}

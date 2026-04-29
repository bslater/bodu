// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
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
        for (int i = 0; i < 10; i++)
            queue.Enqueue(i, i);

        // The last storage slot's element is implementation-defined; remove every element by
        // dequeue order to confirm no corruption.
        Assert.IsTrue(queue.Remove(9));
        Assert.IsFalse(queue.Contains(9));

        var drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.AreEqual(9, drained.Length);
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
    /// Verifies that the heap invariant survives a sequence of removals that triggers both sift-up and sift-down repairs.
    /// </summary>
    [TestMethod]
    public void Remove_WhenManyMixedElements_ShouldMaintainHeapInvariant()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (int i = 0; i < 50; i++)
            queue.Enqueue(i, (i * 13) % 97);

        // Remove a scattered subset.
        foreach (int key in new[] { 0, 5, 10, 15, 25, 35, 49 })
            Assert.IsTrue(queue.Remove(key));

        var drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.AreEqual(50 - 7, drained.Length);
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
}

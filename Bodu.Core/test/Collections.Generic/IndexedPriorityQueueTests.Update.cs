// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Update.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnqueueOrUpdate" /> updates an existing element and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void EnqueueOrUpdate_WhenElementExists_ShouldReturnFalseAndUpdate()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 100);

        Assert.IsFalse(queue.EnqueueOrUpdate("a", 5));
        Assert.AreEqual(5, queue.GetPriority("a"));
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnqueueOrUpdate" /> adds a new element and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void EnqueueOrUpdate_WhenElementIsNew_ShouldReturnTrueAndAdd()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsTrue(queue.EnqueueOrUpdate("a", 1));
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryUpdate" /> returns <see langword="true" /> and re-prioritises an existing element.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenElementExists_ShouldReturnTrueAndUpdate()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 100);
        queue.Enqueue("b", 50);

        Assert.IsTrue(queue.TryUpdate("a", 1));
        Assert.AreEqual("a", queue.Peek().Key);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryUpdate" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = queue.TryUpdate(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TryUpdate" /> returns <see langword="false" /> for a missing element without throwing.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenElementMissing_ShouldReturnFalse()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.IsFalse(queue.TryUpdate("missing", 1));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> respects a reversed
    /// priority comparer so the queue continues to behave as a max-heap after re-prioritisation.
    /// </summary>
    [TestMethod]
    public void Update_WhenComparerIsReversed_ShouldPreserveMaxHeapBehaviour()
    {
        IComparer<int> reverse = Comparer<int>.Create((a, b) => b.CompareTo(a));
        var queue = new IndexedPriorityQueue<string, int>(0, reverse);
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 30);
        queue.Enqueue("c", 20);

        queue.Update("a", 100);

        Assert.AreEqual("a", queue.Peek().Key);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> with a <see langword="null" /> element throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Update_WhenElementIsNull_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            queue.Update(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> on a missing element throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Update_WhenElementMissing_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            queue.Update("missing", 1);
        });
    }

    /// <summary>
    /// Verifies that updating an internal heap node (neither root nor a final leaf) repairs the heap so that
    /// dequeue order remains non-decreasing.
    /// </summary>
    [TestMethod]
    public void Update_WhenInternalNode_ShouldRepairHeap()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (int i = 0; i < 16; i++)
            queue.Enqueue(i, i * 10);

        queue.Update(7, -1);
        queue.Update(3, 999);

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
        Assert.AreEqual(7, drained[0].Key);
        Assert.AreEqual(3, drained[^1].Key);
        Assert.HasCount(16, drained);
    }

    /// <summary>
    /// Verifies that the heap remains valid after a long mixed sequence of Enqueue, Update, and Remove operations.
    /// </summary>
    [TestMethod]
    public void Update_WhenMixedWithEnqueueAndRemove_ShouldMaintainHeapInvariant()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        var rng = new Random(12345);

        for (int i = 0; i < 200; i++)
            queue.Enqueue(i, rng.Next(0, 1_000_000));

        for (int i = 0; i < 200; i += 3)
            queue.Update(i, rng.Next(0, 1_000_000));

        for (int i = 0; i < 200; i += 7)
            queue.Remove(i);

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
    }
    /// <summary>
    /// Verifies that decreasing an element's priority via <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> moves it toward the head.
    /// </summary>
    [TestMethod]
    public void Update_WhenPriorityDecreased_ShouldMoveElementToHead()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 100);
        queue.Enqueue("b", 50);
        queue.Enqueue("c", 75);

        queue.Update("a", 1);

        Assert.AreEqual("a", queue.Peek().Key);
        Assert.AreEqual(1, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that increasing an element's priority via <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> demotes it.
    /// </summary>
    [TestMethod]
    public void Update_WhenPriorityIncreased_ShouldMoveElementAwayFromHead()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 50);
        queue.Enqueue("c", 75);

        queue.Update("a", 1000);

        Assert.AreNotEqual("a", queue.Peek().Key);
        Assert.AreEqual(1000, queue.GetPriority("a"));
    }

    /// <summary>
    /// Verifies that an update that leaves the priority unchanged is a no-op and does not bump the version.
    /// </summary>
    [TestMethod]
    public void Update_WhenPriorityIsUnchanged_ShouldNotInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 5);
        queue.Enqueue("b", 10);

        using IEnumerator<KeyValuePair<string, int>> enumerator = ((IEnumerable<KeyValuePair<string, int>>)queue).GetEnumerator();
        queue.Update("a", 5);
        bool moved = enumerator.MoveNext();

        Assert.IsTrue(moved);
    }

}

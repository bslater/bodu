// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Capacity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> equal to the
    /// existing capacity is a no-op.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenEqualToCurrent_ShouldReturnCurrent()
    {
        var queue = new IndexedPriorityQueue<string, int>(16);

        var newCapacity = queue.EnsureCapacity(16);

        Assert.AreEqual(16, newCapacity);
        Assert.AreEqual(16, queue.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> preserves the
    /// existing element set when growing the backing array.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrowing_ShouldPreserveElements()
    {
        var queue = new IndexedPriorityQueue<int, int>(2);
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, 100 - i);

        _ = queue.EnsureCapacity(64);

        Assert.AreEqual(5, queue.Count);
        for (var i = 0; i < 5; i++)
            Assert.IsTrue(queue.Contains(i));

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
    }
    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> grows the backing array.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenLargerThanCurrent_ShouldGrowBacking()
    {
        var queue = new IndexedPriorityQueue<string, int>(2);

        var newCapacity = queue.EnsureCapacity(64);

        Assert.IsGreaterThanOrEqualTo(64, newCapacity);
        Assert.IsGreaterThanOrEqualTo(64, queue.Capacity);
    }

    /// <summary>
    /// Verifies that a negative argument to <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenNegative_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = queue.EnsureCapacity(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> does not shrink existing capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenSmallerThanCurrent_ShouldPreserveCapacity()
    {
        var queue = new IndexedPriorityQueue<string, int>(64);

        var newCapacity = queue.EnsureCapacity(8);

        Assert.AreEqual(64, newCapacity);
        Assert.AreEqual(64, queue.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.EnsureCapacity" /> with zero on a
    /// non-empty queue returns the existing capacity unchanged.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenZero_ShouldReturnExistingCapacity()
    {
        var queue = new IndexedPriorityQueue<string, int>(8);

        var newCapacity = queue.EnsureCapacity(0);

        Assert.AreEqual(8, newCapacity);
        Assert.AreEqual(8, queue.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TrimExcess" /> is a no-op when the
    /// backing array is already sized exactly to the element count.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityEqualsCount_ShouldBeNoOp()
    {
        var queue = new IndexedPriorityQueue<int, int>(3);
        queue.Enqueue(1, 1);
        queue.Enqueue(2, 2);
        queue.Enqueue(3, 3);
        Assert.AreEqual(3, queue.Capacity);

        queue.TrimExcess();

        Assert.AreEqual(3, queue.Capacity);
        Assert.AreEqual(3, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TrimExcess" /> shrinks the backing array to <c>Count</c>.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCapacityExceedsCount_ShouldShrinkToCount()
    {
        var queue = new IndexedPriorityQueue<string, int>(64);
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        queue.Enqueue("c", 3);

        queue.TrimExcess();

        Assert.AreEqual(3, queue.Capacity);
        Assert.AreEqual(3, queue.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TrimExcess" /> preserves heap order.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenInvoked_ShouldPreserveDequeueOrder()
    {
        var queue = new IndexedPriorityQueue<int, int>(64);
        for (var i = 0; i < 10; i++)
            queue.Enqueue(i, 100 - i);

        queue.TrimExcess();

        KeyValuePair<int, int>[] drained = DrainAll(queue);
        AssertNonDecreasing(drained);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TrimExcess" /> on an empty queue resets capacity to zero.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenQueueIsEmpty_ShouldResetCapacityToZero()
    {
        var queue = new IndexedPriorityQueue<string, int>(64);

        queue.TrimExcess();

        Assert.AreEqual(0, queue.Capacity);
    }

}

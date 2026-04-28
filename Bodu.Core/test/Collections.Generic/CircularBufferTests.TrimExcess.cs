// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.TrimExcess.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that TrimExcess is a no-op when the buffer is completely full, leaving capacity and contents
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsAtCapacity_ShouldBeNoOp()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        buffer.TrimExcess();

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that TrimExcess is a no-op when the buffer is empty and its capacity is already one,
    /// which is the minimum permitted size. This exercises the fixed early-exit condition that previously
    /// allocated a new array unnecessarily in this state.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsEmptyAndCapacityIsOne_ShouldBeNoOp()
    {
        var buffer = new CircularBuffer<int>(1);

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(1, buffer.Capacity);

        buffer.TrimExcess();

        Assert.AreEqual(1, buffer.Capacity, "Capacity should remain 1 when already at the minimum.");
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that TrimExcess reduces the capacity to one when the buffer is empty and its capacity is
    /// greater than one.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsEmpty_ShouldReduceCapacityToOne()
    {
        var buffer = new CircularBuffer<string>(10);

        buffer.TrimExcess();

        Assert.AreEqual(1, buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);
        CollectionAssert.AreEqual(Array.Empty<string>(), buffer.ToArray());
    }

    /// <summary>
    /// Verifies that TrimExcess reduces the capacity to match the current element count when the buffer is
    /// partially filled.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsPartiallyFilled_ShouldReduceCapacityToCount()
    {
        var buffer = new CircularBuffer<int>(10);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        buffer.TrimExcess();

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that TrimExcess preserves FIFO element order when the buffer is wrapped (head &gt;= tail)
    /// prior to trimming.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferIsWrapped_ShouldPreserveFifoOrder()
    {
        // After setup: capacity=4, head=2, tail=1 (wrapped).
        // Logical FIFO order: 3, 4, 5.
        var buffer = new CircularBuffer<int>(4);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        buffer.Enqueue(4);
        buffer.Dequeue(); // removes 1; head=1
        buffer.Dequeue(); // removes 2; head=2
        buffer.Enqueue(5); // wraps into slot 0; tail=1

        buffer.TrimExcess();

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(3, buffer.Count);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that calling TrimExcess a second time on an already-trimmed buffer is a no-op, leaving
    /// capacity and contents unchanged.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalledMultipleTimes_ShouldBeIdempotent()
    {
        var buffer = new CircularBuffer<int>(10);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        buffer.TrimExcess();
        buffer.TrimExcess(); // second call — count == capacity, should short-circuit

        Assert.AreEqual(2, buffer.Capacity);
        Assert.AreEqual(2, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that TrimExcess increments the internal version token when trimming actually occurs, so
    /// that any active enumerator is correctly invalidated.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenTrimmingOccurs_ShouldInvalidateActiveEnumerator()
    {
        var buffer = new CircularBuffer<int>(10);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext(); // advance to element 1

        buffer.TrimExcess(); // capacity reduced from 10 to 3 — version must be bumped

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that TrimExcess does not invalidate an active enumerator when the buffer is already at
    /// capacity and no reallocation occurs.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenNoTrimmingOccurs_ShouldNotInvalidateActiveEnumerator()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3); // buffer is full — count == capacity

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext(); // advance to element 1

        buffer.TrimExcess(); // no-op; version must NOT be bumped

        // MoveNext should succeed, not throw
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, enumerator.Current);
    }

    /// <summary>
    /// Verifies that elements enqueued after TrimExcess are correctly stored and the buffer operates
    /// normally following a trim.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenItemsEnqueuedAfterTrim_ShouldAcceptNewItems()
    {
        var buffer = new CircularBuffer<int>(10, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        buffer.TrimExcess(); // capacity is now 2

        buffer.Enqueue(3); // evicts 1 (allowOverwrite: true, capacity: 2)

        Assert.AreEqual(2, buffer.Capacity);
        Assert.AreEqual(2, buffer.Count);
        CollectionAssert.AreEqual(new[] { 2, 3 }, buffer.ToArray());
    }
}

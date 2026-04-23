// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that the enumerator iterates all elements in FIFO order when the buffer is contiguous
    /// (head &lt; tail).
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferIsContiguous_ShouldIterateInFifoOrder()
    {
        var buffer = new CircularBuffer<int>(5);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.ToList());
    }

    /// <summary>
    /// Verifies that the enumerator iterates all elements in FIFO order when the buffer is wrapped
    /// (head &gt;= tail).
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferIsWrapped_ShouldIterateInFifoOrder()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        buffer.Dequeue();        // head advances to 1
        buffer.Enqueue(4);       // wraps into slot 0

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, buffer.ToList());
    }

    /// <summary>
    /// Verifies that the enumerator yields no elements when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferIsEmpty_ShouldYieldNoElements()
    {
        var buffer = new CircularBuffer<string>(4);
        Assert.AreEqual(0, buffer.ToList().Count);
    }

    /// <summary>
    /// Verifies that Reset repositions the enumerator to before the first element, allowing iteration
    /// to restart from the beginning without modification.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalledWithoutModification_ShouldRestartIteration()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(10);
        buffer.Enqueue(20);
        buffer.Enqueue(30);

        var enumerator = buffer.GetEnumerator();

        // First pass
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
            firstPass.Add(enumerator.Current);

        enumerator.Reset();

        // Second pass
        var secondPass = new List<int>();
        while (enumerator.MoveNext())
            secondPass.Add(enumerator.Current);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, firstPass);
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    /// <summary>
    /// Verifies that accessing Current before the first MoveNext call throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);

        var enumerator = buffer.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    /// <summary>
    /// Verifies that accessing Current after MoveNext has returned false throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.Enqueue(1);

        var enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext()) { }   // exhaust the enumerator

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    // -----------------------------------------------------------------------------------------
    // Enumerator invalidation — Issue 1: Clear() was missing _version++
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that calling Clear during an active enumeration causes the next MoveNext call to throw
    /// <see cref="InvalidOperationException" />, confirming that Clear increments the version token.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearCalledDuringEnumeration_ShouldThrowOnMoveNext()
    {
        var buffer = new CircularBuffer<int>(5);
        for (int i = 1; i <= 5; i++) buffer.Enqueue(i);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext(); // advance to element 1

        buffer.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling Clear before the enumerator is advanced causes the first MoveNext call to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearCalledBeforeFirstMoveNext_ShouldThrowOnMoveNext()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        var enumerator = buffer.GetEnumerator();

        buffer.Clear(); // invalidate before any advance

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling Clear during enumeration causes Reset to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearCalledDuringEnumeration_ShouldThrowOnReset()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext();

        buffer.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.Reset());
    }

    // -----------------------------------------------------------------------------------------
    // Enumerator invalidation — Issue 2: TryDequeueInternal was missing _version++
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that calling Dequeue during an active enumeration causes the next MoveNext call to throw
    /// <see cref="InvalidOperationException" />, confirming that Dequeue increments the version token.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDequeueCalledDuringEnumeration_ShouldThrowOnMoveNext()
    {
        var buffer = new CircularBuffer<int>(5);
        for (int i = 1; i <= 5; i++) buffer.Enqueue(i);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext(); // advance to element 1

        buffer.Dequeue(); // removes element 1; advances head

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling TryDequeue during an active enumeration causes the next MoveNext call to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenTryDequeueCalledDuringEnumeration_ShouldThrowOnMoveNext()
    {
        var buffer = new CircularBuffer<int>(5);
        for (int i = 1; i <= 5; i++) buffer.Enqueue(i);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext();

        buffer.TryDequeue(out _);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling Dequeue before the enumerator is advanced causes the first MoveNext call to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDequeueCalledBeforeFirstMoveNext_ShouldThrowOnMoveNext()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(10);
        buffer.Enqueue(20);

        var enumerator = buffer.GetEnumerator();

        buffer.Dequeue(); // invalidate before any advance

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling Dequeue during enumeration causes Reset to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDequeueCalledDuringEnumeration_ShouldThrowOnReset()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        var enumerator = buffer.GetEnumerator();
        enumerator.MoveNext();

        buffer.Dequeue();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.Reset());
    }

    /// <summary>
    /// Verifies that a successful TryDequeue from an empty buffer — which returns false and makes no structural
    /// change — does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenTryDequeueFailsOnEmptyBuffer_ShouldNotInvalidateEnumerator()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        // Drain the buffer first
        buffer.Dequeue();
        buffer.Dequeue();

        // Now start enumerating the empty buffer, then attempt a no-op TryDequeue
        var enumerator = buffer.GetEnumerator();

        buffer.TryDequeue(out _); // returns false; no version change

        // MoveNext on an empty buffer should return false, not throw
        Assert.IsFalse(enumerator.MoveNext());
    }
}

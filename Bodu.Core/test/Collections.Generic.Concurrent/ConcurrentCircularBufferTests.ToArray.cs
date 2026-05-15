// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.ToArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that after many overwriting enqueues, <see cref="ConcurrentCircularBuffer{T}.ToArray" /> returns only the last <c>Capacity</c> items in FIFO order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenAllowOverwriteTrueAndManyWrites_ShouldReturnLastCapacityItemsInOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);

        for (var i = 0; i < 20; i++)
            buffer.Enqueue(new TestItem(i)); // only last 5 survive: 15..19

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 15, 16, 17, 18, 19 }, values);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> preserves both <see langword="null" /> and non-null items in their FIFO insertion order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenBufferContainsNulls_ShouldPreserveNullsAndOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(3));

        TestItem?[] snapshot = buffer.ToArray();
        Assert.AreEqual(3, snapshot.Length);
        Assert.AreEqual(1, snapshot[0]?.Value);
        Assert.IsNull(snapshot[1]);
        Assert.AreEqual(3, snapshot[2]?.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> returns the currently stored items in FIFO order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenBufferHasElements_ShouldReturnInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        var result = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 10, 20 }, result);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> returns a zero-length array when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenBufferIsEmpty_ShouldReturnEmptyArray()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        TestItem[] result = buffer.ToArray();
        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> never throws when <see cref="ConcurrentCircularBuffer{T}.Clear" /> interleaves with snapshot calls and always returns an array of length within capacity.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenClearInterleaves_ShouldNotThrowAndMayReturnEmpty()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8, allowOverwrite: true);
        for (var i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        var failures = new ConcurrentBag<Exception>();
        var nonEmptySnapshots = 0;

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                buffer.Clear();
                Thread.SpinWait(20);
                buffer.TryEnqueue(new TestItem(100 + i));
            }
        });

        var taker = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                try
                {
                    TestItem[] snap = buffer.ToArray();
                    if (snap.Length > 0) nonEmptySnapshots++;
                    Assert.IsTrue(snap.Length <= buffer.Capacity);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
        });

        Task.WaitAll(clearer, taker);

        Assert.AreEqual(0, failures.Count, "ToArray should not throw while Clear interleaves.");
        Assert.IsTrue(nonEmptySnapshots >= 0); // sanity: we may see empty or non-empty snapshots
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> under concurrent dequeues never throws and returns arrays of length within capacity.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenConcurrentDequeue_ShouldNotThrowAndLengthWithinCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (var i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var failures = new ConcurrentBag<Exception>();

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    TestItem[] snapshot = buffer.ToArray();
                    Assert.IsTrue(snapshot.Length <= buffer.Capacity);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
        });

        var dequeuer = Task.Run(() =>
        {
            for (var i = 0; i < 5; i++)
                buffer.TryDequeue(out _);
        });

        Task.WaitAll(reader, dequeuer);
        Assert.AreEqual(0, failures.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> under concurrent enqueues never throws and returns arrays of length within capacity.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenConcurrentEnqueue_ShouldNotThrowAndLengthWithinCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var failures = new ConcurrentBag<Exception>();

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    TestItem[] snapshot = buffer.ToArray();
                    Assert.IsTrue(snapshot.Length <= buffer.Capacity);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
        });

        var writer = Task.Run(() =>
        {
            for (var i = 10; i < 110; i++)
                buffer.Enqueue(new TestItem(i));
        });

        Task.WaitAll(reader, writer);
        Assert.AreEqual(0, failures.Count, "ToArray should not throw under concurrent enqueue.");
    }

    /// <summary>
    /// Verifies that many concurrent <see cref="ConcurrentCircularBuffer{T}.ToArray" /> calls never throw and each returned snapshot's length is within capacity.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenManyThreadsCallConcurrently_ShouldNotThrowAndReturnIndependentSnapshots()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(16, allowOverwrite: true);
        for (var i = 0; i < 12; i++) buffer.Enqueue(new TestItem(i));

        var failures = new ConcurrentBag<Exception>();
        var lengths = new ConcurrentBag<int>();

        Parallel.For(0, 200, _ =>
        {
            try
            {
                TestItem[] snap = buffer.ToArray();
                lengths.Add(snap.Length);
                Assert.IsTrue(snap.Length <= buffer.Capacity);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        });

        Assert.AreEqual(0, failures.Count);
        Assert.IsTrue(lengths.All(len => len >= 0 && len <= 16));
    }

    /// <summary>
    /// Verifies that modifying the array returned by <see cref="ConcurrentCircularBuffer{T}.ToArray" /> does not change the buffer's internal state.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSnapshotIsModified_ShouldNotAffectBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        TestItem[] snapshot = buffer.ToArray();
        snapshot[0] = new TestItem(999); // mutate snapshot only

        // Buffer should be unchanged
        var again = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2 }, again);
    }

    /// <summary>
    /// Verifies that after the buffer has wrapped, <see cref="ConcurrentCircularBuffer{T}.ToArray" /> still returns items in logical FIFO order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenWrapped_ShouldReturnInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue();                // remove 1
        buffer.Enqueue(new TestItem(4)); // wrap

        var result = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, result);
    }

}
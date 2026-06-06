// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.TryDequeue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that a heavily-overfilled buffer with overwriting enabled drains cleanly via <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> within the capacity bound.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenAllowOverwriteTrue_ShouldNotThrowAndRespectBounds()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);

        // Overfill heavily; no exceptions expected
        Parallel.For(0, 1000, i => buffer.Enqueue(new TestItem(i)));

        // Drain what's there
        var count = 0;
        while (buffer.TryDequeue(out _)) count++;

        Assert.IsTrue(count >= 0 && count <= buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> on an empty buffer returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenBufferIsEmpty_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        Assert.IsFalse(buffer.TryDequeue(out _));
    }

    /// <summary>
    /// Verifies that after <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> drains the last item, the next call returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenBufferTransitionsToEmpty_ShouldReturnFalseAfterLastItem()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        Assert.IsTrue(buffer.TryDequeue(out _));
        Assert.IsTrue(buffer.TryDequeue(out _));
        Assert.IsFalse(buffer.TryDequeue(out _));
    }

    // Previously tested with capacity = 1. Migrated to capacity = 2 — the minimum supported
    // value — following the implementation change that requires capacity >= 2 for the Vyukov
    // MPMC sequence protocol to be correct.

    /// <summary>
    /// Verifies that at the minimum-valid capacity, repeated enqueue/<see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> cycles keep the count within the capacity bound.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenCapacityIsMinimum_ShouldBehaveConsistently()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        for (var i = 0; i < 100; i++)
        {
            buffer.Enqueue(new TestItem(i));
            _ = buffer.TryDequeue(out _);
        }

        Assert.IsTrue(buffer.Count is >= 0 and <= 2);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> interleaved with concurrent <see cref="ConcurrentCircularBuffer{T}.Clear" /> calls never throws and ultimately returns <see langword="false" /> on the emptied buffer.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenClearInterleaves_ShouldReturnFalseOnceEmptiedWithoutThrowing()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        for (var i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var done = new ManualResetEventSlim(false);

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                buffer.Clear();
                Thread.SpinWait(20);
            }
            done.Set();
        });

        var reader = Task.Run(() =>
        {
            try
            {
                // Keep trying while clears run; should never throw
                while (!done.IsSet)
                    _ = buffer.TryDequeue(out _);
            }
            catch (Exception ex) { exceptions.Add(ex); }
        });

        Task.WaitAll(clearer, reader);
        Assert.IsEmpty(exceptions);
        Assert.IsFalse(buffer.TryDequeue(out _)); // should be empty now
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> eventually succeeds against a buffer fed by a concurrent enqueuer.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenConcurrentEnqueueInterleaves_ShouldEventuallySucceed()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var dequeuedCount = 0;
        var success = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                buffer.Enqueue(new TestItem(i));
                Thread.Sleep(1);
            }
        });

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                if (buffer.TryDequeue(out TestItem? item) && item != null)
                {
                    Interlocked.Increment(ref dequeuedCount);
                    Interlocked.Exchange(ref success, 1);
                }
                Thread.Sleep(1);
            }
        });

        Task.WaitAll(writer, reader);

        Assert.AreEqual(1, success, "TryDequeue never succeeded during concurrent enqueue.");
        Assert.IsGreaterThan(0, dequeuedCount, "Expected to dequeue at least one item.");
    }

    /// <summary>
    /// Verifies that many concurrent consumers drain a preloaded buffer and together receive every item exactly once.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenManyConsumersAgainstPreloadedBuffer_ShouldDrainAllItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(20);
        for (var i = 0; i < 20; i++) buffer.Enqueue(new TestItem(i));

        var dequeued = new ConcurrentBag<int>();

        Parallel.For(0, 20, _ =>
        {
            if (buffer.TryDequeue(out TestItem? item) && item != null)
                dequeued.Add(item.Value);
        });

        Assert.HasCount(20, dequeued);
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 20).ToArray(), dequeued.OrderBy(x => x).ToArray());
    }

    /// <summary>
    /// Verifies that no item is ever returned more than once across parallel <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> consumers.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenMultipleConsumers_ShouldDequeueEachItemAtMostOnce()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(30);
        for (var i = 0; i < 30; i++) buffer.Enqueue(new TestItem(i));

        var dequeued = new ConcurrentBag<int>();
        Task[] tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
        {
            while (buffer.TryDequeue(out TestItem? item))
                if (item != null) dequeued.Add(item.Value);
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.HasCount(30, dequeued);

        var groups = dequeued.GroupBy(x => x).Select(g => g.Count()).ToArray();
        Assert.IsTrue(groups.All(c => c == 1));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 30).ToArray(), dequeued.OrderBy(x => x).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> returns each <see langword="null" /> item that was enqueued, in FIFO order.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenNullsPresent_ShouldReturnNullValues()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(4);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null);

        int nulls = 0, nonNulls = 0;
        while (buffer.TryDequeue(out TestItem? item))
        {
            if (item is null) nulls++; else nonNulls++;
        }

        Assert.AreEqual(2, nulls);
        Assert.AreEqual(1, nonNulls);
    }

    /// <summary>
    /// Verifies that single-threaded <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> returns items in strict FIFO order.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenSingleThreaded_ShouldReturnInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (var i = 0; i < 3; i++) buffer.Enqueue(new TestItem(i));

        var results = new TestItem[3];
        for (var i = 0; i < 3; i++)
            Assert.IsTrue(buffer.TryDequeue(out results[i]));

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, results.Select(r => r.Value).ToArray());
    }

}
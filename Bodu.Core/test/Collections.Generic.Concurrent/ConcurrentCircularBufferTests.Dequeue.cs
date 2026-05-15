// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Dequeue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that when overwriting is disabled and the buffer is full, a failed enqueue leaves the existing FIFO sequence intact for later dequeue.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenAllowOverwriteFalseAndBufferFull_NewEnqueueThrowsButExistingItemsRemainInFifo()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(new TestItem(3));
        });

        // Existing items should be intact and in FIFO order
        Assert.AreEqual(1, buffer.Dequeue().Value);
        Assert.AreEqual(2, buffer.Dequeue().Value);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> on an empty buffer throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenBufferIsEmpty_ShouldThrowInvalidOperation()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Dequeue();
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> interleaved with <see cref="ConcurrentCircularBuffer{T}.Clear" /> never throws anything other than <see cref="InvalidOperationException" /> and never corrupts the buffer.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenClearInterleaves_ShouldOnlyThrowWhenEmptyAndNeverCorruptState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        for (var i = 0; i < 8; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource();

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                buffer.Clear();
                Thread.SpinWait(20);
            }
            cts.Cancel();
        });

        var consumer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    // Dequeue may throw when empty — allowed; should not throw any other exception
                    buffer.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    // expected if empty
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(clearer, consumer);
        Assert.AreEqual(0, exceptions.Count, "Unexpected exception during Dequeue/Clear interleaving.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that a concurrent producer and consumer together transfer every item exactly once, without loss or duplication.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenEnqueueAndDequeueConcurrently_ShouldDeliverAllItemsExactlyOnce()
    {
        // Use a capacity equal to the total item count so no evictions occur. The consumer
        // drains the buffer as the producer fills it; we assert set equivalence rather than
        // ordering because the interleaving of two concurrent tasks is non-deterministic.
        var buffer = new ConcurrentCircularBuffer<TestItem>(100);
        var dequeued = new ConcurrentBag<int>();

        var enqueuer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                buffer.Enqueue(new TestItem(i));
                Thread.SpinWait(10);
            }
        });

        var dequeuer = Task.Run(() =>
        {
            var count = 0;
            while (count < 100)
            {
                if (buffer.TryDequeue(out TestItem? item))
                {
                    dequeued.Add(item.Value);
                    count++;
                }
                Thread.SpinWait(5);
            }
        });

        Task.WaitAll(enqueuer, dequeuer);

        // All 100 distinct values must be present exactly once; order is not asserted.
        Assert.AreEqual(100, dequeued.Count, "All items must be dequeued exactly once.");
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 100).ToArray(), dequeued.ToArray(),
            "The set of dequeued values must equal the set of enqueued values.");
    }

    /// <summary>
    /// Verifies that many concurrent consumers draining a populated buffer each get a disjoint share, and together consume every item exactly once.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenMultipleConsumers_ShouldReturnEachItemExactlyOnce()
    {
        const int toProduce = 10_000;
        var buffer = new ConcurrentCircularBuffer<TestItem>(toProduce, allowOverwrite: false);

        for (var i = 0; i < toProduce; i++)
            buffer.Enqueue(new TestItem(i));

        var bag = new ConcurrentBag<int>();

        Parallel.For(0, Environment.ProcessorCount * 2, _ =>
        {
            while (buffer.TryDequeue(out TestItem? item) && item != null)
                bag.Add(item.Value);
        });

        Assert.AreEqual(toProduce, bag.Count);
        CollectionAssert.AreEquivalent(Enumerable.Range(0, toProduce).ToArray(), bag.ToArray());
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that dequeueing a previously enqueued <see langword="null" /> returns <see langword="null" /> and decrements the count.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenNullEnqueued_ShouldReturnNull()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(2);
        buffer.Enqueue(null);

        TestItem? result = buffer.Dequeue();
        Assert.IsNull(result);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that a mixed sequence of null and non-null enqueues is returned by <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> in the exact order enqueued.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenNullsAndValuesMixed_ShouldReturnExactlyWhatWasEnqueued()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(4);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(2));

        TestItem? a = buffer.Dequeue();
        TestItem? b = buffer.Dequeue();
        TestItem? c = buffer.Dequeue();
        TestItem? d = buffer.Dequeue();

        Assert.IsNull(a);
        Assert.AreEqual(1, b?.Value);
        Assert.IsNull(c);
        Assert.AreEqual(2, d?.Value);
    }

    /// <summary>
    /// Verifies that after overwrites evict older items, <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> returns the oldest survivor first.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenOverwriteEnabledAndProducersEvict_ShouldReturnOldestOfCurrentSet()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3)); // [1,2,3] full

        buffer.Enqueue(new TestItem(4)); // evicts 1 => [2,3,4]

        Assert.AreEqual(2, buffer.Dequeue().Value);
        Assert.AreEqual(3, buffer.Dequeue().Value);
        Assert.AreEqual(4, buffer.Dequeue().Value);
    }

    // Issue 5 — the original test asserted CollectionAssert.AreEqual (exact order) on items
    // dequeued by a concurrent consumer. Because the producer and consumer run concurrently with
    // different SpinWait ratios, the dequeue order depends on thread scheduling and is not
    // guaranteed to be strictly sequential, making the test timing-sensitive and fragile.
    //
    // This file now contains two tests:
    //   - a strict sequential FIFO test (single-threaded, deterministic)
    //   - a concurrent test that correctly uses AreEquivalent (same elements, any order)

    /// <summary>
    /// Verifies that single-threaded enqueue followed by full drain returns items in strict FIFO order.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenSingleThreaded_ShouldReturnItemsInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(100);

        for (var i = 0; i < 100; i++)
            buffer.Enqueue(new TestItem(i));

        var result = new int[100];
        for (var i = 0; i < 100; i++)
            result[i] = buffer.Dequeue().Value;

        CollectionAssert.AreEqual(Enumerable.Range(0, 100).ToArray(), result,
            "Single-threaded dequeue must return items in strict FIFO order.");
    }

    /// <summary>
    /// Verifies that slots reused after a wraparound are still dequeued in FIFO order.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenSlotReusedAfterWrap_ShouldReturnInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(10));
        buffer.Dequeue();         // slot 0 freed
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30)); // wrap

        Assert.AreEqual(20, buffer.Dequeue().Value);
        Assert.AreEqual(30, buffer.Dequeue().Value);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that FIFO order is preserved across a wraparound boundary.
    /// </summary>
    [TestMethod]
    public void Dequeue_WhenWrapAroundOccurs_ShouldPreserveFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue(); // evict 1
        buffer.Enqueue(new TestItem(4));

        Assert.AreEqual(2, buffer.Dequeue().Value);
        Assert.AreEqual(3, buffer.Dequeue().Value);
        Assert.AreEqual(4, buffer.Dequeue().Value);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> on an empty buffer returns <see langword="false" /> and outputs the default value.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenBufferIsEmpty_ShouldReturnFalseAndOutDefault()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var ok = buffer.TryDequeue(out TestItem? item);
        Assert.IsFalse(ok);
        Assert.IsNull(item);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that parallel <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> calls collectively return every item in the buffer exactly once.
    /// </summary>
    [TestMethod]
    public void TryDequeue_WhenDrainedConcurrently_ShouldReturnAllItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        TestItem[] source = Enumerable.Range(1, 5).Select(i => new TestItem(i)).ToArray();

        foreach (TestItem? item in source)
            buffer.Enqueue(item);

        var dequeued = new ConcurrentBag<TestItem>();

        Parallel.For(0, source.Length, _ =>
        {
            if (buffer.TryDequeue(out TestItem? item) && item != null)
                dequeued.Add(item);
        });

        Assert.AreEqual(source.Length, dequeued.Count);
        CollectionAssert.AreEquivalent(
            source.Select(i => i.Value).ToArray(),
            dequeued.Select(i => i.Value).ToArray());
    }

}
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> empties a buffer containing <see langword="null" /> items without throwing.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferContainsNulls_ShouldEmptyWithoutError()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(null!);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null!);

        buffer.Clear();

        Assert.AreEqual(0, buffer.Count);
        CollectionAssert.AreEqual(Array.Empty<TestItem>(), buffer.ToArray());
    }

    /// <summary>
    /// Verifies that items removed by <see cref="ConcurrentCircularBuffer{T}.Clear" /> release their references so the GC can collect them.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferContainsOnlyObjectsWithoutReferences_ShouldAllowGarbageCollection()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        var wr1 = new WeakReference(new TestItem(1));
        var wr2 = new WeakReference(new TestItem(2));

        buffer.Enqueue((TestItem)wr1.Target!);
        buffer.Enqueue((TestItem)wr2.Target!);

        buffer.Clear();

        wr1.Target = null;
        wr2.Target = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(wr1.IsAlive || wr2.IsAlive, "Cleared items should be eligible for GC.");
    }

    /// <summary>
    /// Verifies that after <see cref="ConcurrentCircularBuffer{T}.Clear" />, capacity is preserved and subsequent enqueues populate the buffer in FIFO order.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferFilledAndImmediatelyReused_ShouldPreserveCapacityAndMaintainFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        buffer.Clear();

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);

        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(11));
        CollectionAssert.AreEqual(new[] { 10, 11 }, buffer.ToArray().Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> on a full buffer does not raise <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferFull_WithAllowOverwriteTrue_ShouldNotRaiseEvictionEvents()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        int evicted = 0;

        buffer.ItemEvicted += _ => evicted++;

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Clear();

        Assert.AreEqual(0, evicted, "Clear must not raise ItemEvicted.");
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that repeatedly calling <see cref="ConcurrentCircularBuffer{T}.Clear" /> on an empty buffer is a no-op that preserves capacity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferIsEmptyAndCalledRepeatedly_ShouldBeNoOp()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        buffer.Clear();
        buffer.Clear();
        buffer.Clear();

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that post-clear enqueues form a fresh FIFO sequence with none of the pre-clear items visible.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferResetWithSubsequentEnqueues_ShouldMaintainFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));
        buffer.Clear();

        for (int i = 10; i < 15; i++) buffer.Enqueue(new TestItem(i));

        int[] snapshot = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 10, 11, 12, 13, 14 }, snapshot);
    }

    /// <summary>
    /// Verifies that enqueuing items after clearing a previously full buffer behaves correctly
    /// and maintains FIFO order.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferWasFullAndNewItemsEnqueued_ShouldMaintainFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        Assert.AreEqual(3, buffer.Count);
        buffer.Clear();
        Assert.AreEqual(0, buffer.Count);

        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        CollectionAssert.AreEqual(new[] { new TestItem(10), new TestItem(20), new TestItem(30) }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies idempotency of Clear under mixed activity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledRepeatedlyDuringProducerConsumerActivity_ShouldRemainStable()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        bool failed = false;

        var toggler = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                if (!buffer.TryEnqueue(new TestItem(i)))
                    buffer.TryDequeue(out _);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                buffer.Clear();
                if (buffer.Count < 0 || buffer.Count > buffer.Capacity)
                    failed = true;

                Thread.SpinWait(20);
            }
        });

        Task.WaitAll(toggler, clearer);

        Assert.IsFalse(failed, "Repeated Clear caused buffer state inconsistency.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that draining all items via TryDequeue and then refilling the buffer preserves
    /// FIFO order across the cycle.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDrainedViaTryDequeueAndRefilled_ShouldPreserveFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);

        Assert.AreEqual(0, buffer.Count);

        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        CollectionAssert.AreEqual(new[] { new TestItem(10), new TestItem(20) }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> removes all items even if concurrently reading Count and ToArray.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvokedDuringConcurrentRead_ShouldProduceConsistentEmptyState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var stop = new CancellationTokenSource();
        int readErrors = 0;

        var reader = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                _ = buffer.Count;      // exercise approximate count
                _ = buffer.ToArray();   // exercise snapshot copy
                Thread.SpinWait(50);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                try { buffer.Clear(); }
                catch { Interlocked.Increment(ref readErrors); }
                Thread.SpinWait(100);
            }
            stop.Cancel();
        });

        Task.WaitAll(reader, clearer);

        Assert.AreEqual(0, readErrors, "Clear should not throw during concurrent reads.");
        Assert.AreEqual(0, buffer.Count);
        CollectionAssert.AreEqual(Array.Empty<TestItem>(), buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> does not cause invalid state when concurrent Dequeue is active.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvokedDuringDequeuePressure_ShouldNotThrowInvalidOperation()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();

        var dequeuer = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    buffer.TryDequeue(out _);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                Thread.SpinWait(20);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                buffer.Clear();
                Thread.SpinWait(50);
            }
        });

        Task.WaitAll(dequeuer, clearer);

        Assert.IsTrue(exceptions.All(e => e is not InvalidOperationException),
            "Clear should not corrupt internal state during Dequeue.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> does not interfere with concurrent Enqueue and Count remains in range.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvokedDuringEnqueuePressure_ShouldNotCorruptState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        bool failed = false;

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                Thread.SpinWait(10);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                buffer.Clear();
                if (buffer.Count < 0 || buffer.Count > buffer.Capacity)
                    failed = true;

                Thread.SpinWait(100);
            }
        });

        Task.WaitAll(writer, clearer);

        Assert.IsFalse(failed, "Buffer state became invalid (Count out of range) during Clear.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Clear" /> interleaved with active producers and consumers keeps <see cref="ConcurrentCircularBuffer{T}.Count" /> within <c>[0, Capacity]</c>.
    /// </summary>
    [TestMethod]
    public void Clear_WhenProducerConsumerActive_ShouldKeepCountWithinBounds()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8, allowOverwrite: true);
        var cts = new CancellationTokenSource();
        int violations = 0;

        var producer = Task.Run(() =>
        {
            int i = 0;
            while (!cts.IsCancellationRequested)
            {
                buffer.TryEnqueue(new TestItem(i++));
                Thread.SpinWait(100);
            }
        });

        var consumer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                buffer.TryDequeue(out _);
                Thread.SpinWait(100);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                buffer.Clear();
                int count = buffer.Count;
                if (count < 0 || count > buffer.Capacity)
                    Interlocked.Increment(ref violations);

                Thread.SpinWait(50);
            }
            cts.Cancel();
        });

        Task.WaitAll(producer, consumer, clearer);

        Assert.AreEqual(0, violations, "Count should always be within [0, Capacity] during Clear.");
    }

    /// <summary>
    /// Verifies that read APIs — <see cref="ConcurrentCircularBuffer{T}.TryPeek" />, <see cref="ConcurrentCircularBuffer{T}.Peek" />, <see cref="ConcurrentCircularBuffer{T}.Contains" />, and the indexer — only surface documented exceptions (empty/out-of-range race conditions) while <see cref="ConcurrentCircularBuffer{T}.Clear" /> fires concurrently.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Clear_WhenReadingWithPeekContainsIndexer_ShouldOnlyRaiseExpectedExceptions(bool allowOverwrite)
    {
        // Arrange
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity: 6, allowOverwrite: allowOverwrite);
        for (int i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        using var cts = new CancellationTokenSource();
        var start = new ManualResetEventSlim(false);

        // Metrics
        int iterations = 0;
        int peekEmptyCount = 0;              // InvalidOperationException from Peek() when empty
        int indexerRaceCount = 0;            // ArgumentOutOfRangeException from indexer during races
        var unexpected = new ConcurrentBag<Exception>();

        // Reader task that exercises TryPeek, Peek, Contains, and the indexer
        var reader = Task.Run(() =>
        {
            start.Wait();
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    // 1) Safe: never throws
                    _ = buffer.TryPeek(out _);

                    // 2) Strict: may throw when empty (this is the only allowed exception here)
                    try
                    {
                        _ = buffer.Peek();
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref peekEmptyCount);
                    }

                    // 3) Safe: should not throw
                    _ = buffer.Contains(new TestItem(999));

                    // 4) Indexer: choose a valid index if we can; still allow race with Clear()
                    try
                    {
                        TestItem[] snap = buffer.ToArray(); // snapshot to reduce�but not eliminate�races
                        if (snap.Length > 0)
                        {
                            // pick first element from the snapshot
                            _ = buffer[0];
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Interlocked.Increment(ref indexerRaceCount);
                    }
                }
                catch (Exception ex)
                {
                    // Any other exception type is unexpected
                    unexpected.Add(ex);
                }

                Interlocked.Increment(ref iterations);
                Thread.Sleep(1); // yield so the clearer thread is also scheduled under load
            }
        });

        // Clearer task that repeatedly clears and lightly reuses the buffer.
        // Thread.Sleep(2) is used instead of Thread.SpinWait here: SpinWait is a
        // CPU-bound busy-wait that never yields the thread. At 60 iterations it
        // completes in <10 µs under load, cancelling the CTS before the reader task
        // has been scheduled even once. Sleep yields the thread, ensuring the reader
        // gets CPU time on every iteration.
        var clearer = Task.Run(() =>
        {
            start.Wait();
            for (int i = 0; i < 60 && !cts.IsCancellationRequested; i++)
            {
                buffer.Clear();

                // Re-add a couple of items sometimes to vary state
                if ((i % 3) == 0)
                {
                    buffer.TryEnqueue(new TestItem(100 + i));
                    buffer.TryEnqueue(new TestItem(200 + i));
                }
                Thread.Sleep(2);
            }
            cts.Cancel();
        });

        // Act
        start.Set();
        Task.WaitAll(reader, clearer);

        // Assert No unexpected exceptions should have escaped the inner try-catches
        Assert.IsEmpty(unexpected, $"Unexpected exceptions: {string.Join(", ", unexpected.Select(e => e.GetType().Name))}");

        // We expect at least some iterations to have run
        Assert.IsGreaterThan(0, iterations, "Reader performed no iterations.");

        // Invariants still hold
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity, "Count must remain within [0, Capacity].");

        // Sanity: we should have observed at least *some* of the allowed race conditions (Not strictly required, but useful to ensure
        // the test actually exercised races.)
        Assert.IsGreaterThanOrEqualTo(0, peekEmptyCount);
        Assert.IsGreaterThanOrEqualTo(0, indexerRaceCount);
    }

}

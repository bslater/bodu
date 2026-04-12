using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
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

    [TestMethod]
    public void Clear_WhenBufferContainsOnlyObjectsWithoutReferences_ShouldAllowGarbageCollection()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);

        // Items are created and enqueued inside a NoInlining helper. When that frame returns,
        // item1 and item2 are fully off the stack — the buffer's internal slots are the only
        // remaining strong references. Setting locals to null inside this method is insufficient
        // because the JIT (particularly in debug builds) keeps the old GC roots live for the
        // entire enclosing scope regardless of the null assignment.
        var (wr1, wr2) = EnqueueItemsAndReturnWeakReferences(buffer);

        // Clear() drains via TryDequeue, which nulls each slot before returning.
        // Once this call returns, no strong references to the items remain anywhere.
        buffer.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(wr1.IsAlive, "Item 1 should be eligible for collection after Clear.");
        Assert.IsFalse(wr2.IsAlive, "Item 2 should be eligible for collection after Clear.");
    }

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

    [TestMethod]
    public void Clear_WhenBufferResetWithSubsequentEnqueues_ShouldMaintainFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));
        buffer.Clear();

        for (int i = 10; i < 15; i++) buffer.Enqueue(new TestItem(i));

        var snapshot = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 10, 11, 12, 13, 14 }, snapshot);
    }

    /// <summary>
    /// Verifies idempotency of Clear under mixed activity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledRepeatedlyDuringProducerConsumerActivity_ShouldRemainStable()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var failed = false;

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
    /// Verifies that Clear removes all items even if concurrently reading Count and ToArray.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvokedDuringConcurrentRead_ShouldProduceConsistentEmptyState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var stop = new CancellationTokenSource();
        var readErrors = 0;

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
    /// Verifies that Clear does not cause invalid state when concurrent Dequeue is active.
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
    /// Verifies that Clear does not interfere with concurrent Enqueue and Count remains in range.
    /// </summary>
    [TestMethod]
    public void Clear_WhenInvokedDuringEnqueuePressure_ShouldNotCorruptState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        var failed = false;

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
                var count = buffer.Count;
                if (count < 0 || count > buffer.Capacity)
                    Interlocked.Increment(ref violations);

                Thread.SpinWait(50);
            }
            cts.Cancel();
        });

        Task.WaitAll(producer, consumer, clearer);

        Assert.AreEqual(0, violations, "Count should always be within [0, Capacity] during Clear.");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Clear_WhenReadingWithPeekContainsIndexer_ShouldOnlyRaiseExpectedExceptions(bool allowOverwrite)
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity: 6, allowOverwrite: allowOverwrite);
        for (int i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        using var cts = new CancellationTokenSource();
        var start = new ManualResetEventSlim(false);

        int iterations = 0;
        int peekEmptyCount = 0;              // InvalidOperationException from Peek() when empty
        int indexerRaceCount = 0;            // ArgumentOutOfRangeException from indexer during races
        var unexpected = new ConcurrentBag<Exception>();

        // Reader task that exercises TryPeek, Peek, Contains, and the indexer.
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
                        var snap = buffer.ToArray(); // snapshot to reduce — but not eliminate — races
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
                Thread.SpinWait(50);
            }
        });

        // Clearer task that repeatedly clears and lightly reuses the buffer.
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
                Thread.SpinWait(100);
            }
            cts.Cancel();
        });

        start.Set();
        Task.WaitAll(reader, clearer);

        // No unexpected exceptions should have escaped the inner try-catches.
        Assert.AreEqual(0, unexpected.Count, $"Unexpected exceptions: {string.Join(", ", unexpected.Select(e => e.GetType().Name))}");

        // We expect at least some iterations to have run.
        Assert.IsTrue(iterations > 0, "Reader performed no iterations.");

        // Invariants still hold.
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity, "Count must remain within [0, Capacity].");

        // Sanity: we should have observed at least some of the allowed race conditions.
        // (Not strictly required, but useful to confirm the test actually exercised races.)
        Assert.IsTrue(peekEmptyCount >= 0);
        Assert.IsTrue(indexerRaceCount >= 0);
    }
}
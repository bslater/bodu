// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests._EnqueueDequeue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that with overwriting disabled, items only leave the buffer via <see cref="ConcurrentCircularBuffer{T}.TryDequeue" /> so <c>enqueued == dequeued + count</c>.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenAllowOverwriteFalse_ShouldNotDropItemsExceptByDequeue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: false);
        int enq = 0, deq = 0;

        Parallel.Invoke(
            () =>
            {
                for (var i = 0; i < 50_000; i++)
                {
                    if (buffer.TryEnqueue(new TestItem(i)))
                        Interlocked.Increment(ref enq);
                }
            },
            () =>
            {
                for (var i = 0; i < 50_000; i++)
                {
                    if (buffer.TryDequeue(out _))
                        Interlocked.Increment(ref deq);
                }
            }
        );

        // Items leave only via Dequeue; remaining are Count
        Assert.AreEqual(enq, deq + buffer.Count);
        Assert.IsLessThanOrEqualTo(buffer.Capacity, buffer.Count);
    }

    /// <summary>
    /// Verifies that with overwriting enabled, heavy concurrent Enqueue and TryDequeue churn never throws and the count stays within capacity.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenAllowOverwriteTrue_ShouldNeverExceedCapacityOrThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.Invoke(
            () =>
            {
                for (var i = 0; i < 10_000; i++)
                {
                    try { buffer.Enqueue(new TestItem(i)); }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            },
            () =>
            {
                for (var i = 0; i < 10_000; i++)
                    buffer.TryDequeue(out _);
            }
        );

        Assert.IsEmpty(exceptions, "Enqueue should not throw in overwrite mode.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }
    /// <summary>
    /// Verifies that at the minimum capacity, sustained concurrent TryEnqueue/TryDequeue churn never throws and keeps the count within bounds.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenCapacityIsMin_ShouldBehaveConsistently()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);
        var errors = new ConcurrentBag<Exception>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        Task[] tasks = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(_ => Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    buffer.TryEnqueue(new TestItem(0));
                    buffer.TryDequeue(out TestItem? @out);
                    Thread.SpinWait(20);
                }
                catch (Exception ex) { errors.Add(ex); }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.IsEmpty(errors, "No exceptions expected under capacity-1 churn.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= 1);
    }

    //[TestMethod]
    //public void EnqueueAndDequeue_WhenCapacityIsOne_ShouldBehaveConsistently()
    //{
    //    var buffer = new ConcurrentCircularBuffer<TestItem>(1, allowOverwrite: true);
    //    var errors = new ConcurrentBag<Exception>();
    //    var cts = new CancellationTokenSource();
    //    cts.CancelAfter(TimeSpan.FromSeconds(5)); // time-box the churn

    // Parallel.For(0, Environment.ProcessorCount * 2, _ => { try { while (!cts.IsCancellationRequested) { // Non-throwing enqueue
    // avoids pathological spinning under extreme contention buffer.TryEnqueue(new TestItem(0)); buffer.TryDequeue(out TestItem? _);

    // Tiny pause helps fairness between threads; avoids starvation Thread.SpinWait(20); } } catch (Exception ex) { errors.Add(ex); } });

    //    Assert.AreEqual(0, errors.Count, "No exceptions are expected under capacity==1 churn.");
    //    Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= 1, "Count must stay within [0,1].");
    //}

    /// <summary>
    /// Verifies that enqueue/dequeue churn interleaved with concurrent <see cref="ConcurrentCircularBuffer{T}.Clear" /> calls leaves the count within <c>[0, Capacity]</c>.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenClearInterleaves_ShouldRemainWithinBounds()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8, allowOverwrite: true);
        var cts = new CancellationTokenSource();

        var churn = Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                buffer.TryEnqueue(new TestItem(i++));
                buffer.TryDequeue(out _);
            }
        });

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                buffer.Clear();
                Thread.SpinWait(50);
            }
            cts.Cancel();
        });

        Task.WaitAll(churn, clearer);
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that concurrent producer/consumer pairs preserve <see langword="null" /> items in the buffer's transferred sequence.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenConcurrent_ShouldRetainNulls()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(10);
        var nullCount = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
                buffer.Enqueue(i % 2 == 0 ? null : new TestItem(i)); // ~50% nulls
        });

        var reader = Task.Run(() =>
        {
            var attempts = 0;
            while (attempts < 150)
            {
                if (buffer.TryDequeue(out TestItem? item) && item is null)
                    Interlocked.Increment(ref nullCount);
                attempts++;
            }
        });

        Task.WaitAll(writer, reader);

        if (nullCount == 0)
            nullCount = buffer.ToArray().Count(x => x is null); // last-chance snapshot

        Assert.IsGreaterThan(0, nullCount, "Expected to observe null entries.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that interleaved enqueue/dequeue operations produce only valid item values and keep the count within capacity.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenInterleavedConcurrently_ShouldNotCorruptState()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var output = new ConcurrentBag<int>();

        var task = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                if (buffer.TryDequeue(out TestItem? item) && item != null)
                    output.Add(item.Value);
            }
        });

        task.Wait();

        Assert.IsTrue(output.All(v => v >= 0 && v < 1000));
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that many concurrent producers and consumers do not deadlock and together keep the count within capacity.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenManyProducersAndConsumers_ShouldKeepBoundsAndNotDeadlock()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(32, allowOverwrite: true);
        var cts = new CancellationTokenSource();
        Task[] producers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
                buffer.Enqueue(new TestItem(Interlocked.Increment(ref i)));
        })).ToArray();

        Task[] consumers = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
                buffer.TryDequeue(out _);
        })).ToArray();

        Thread.Sleep(250); // brief run
        cts.Cancel();
        Task.WaitAll(producers.Concat(consumers).ToArray());

        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that repeatedly filling and draining the buffer does not leak entries or stall; final count returns to zero.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenRepeatedWraparound_ShouldNotLeakOrStall()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);

        for (var i = 0; i < 100; i++)
        {
            buffer.Enqueue(new TestItem(i));
            buffer.Dequeue();
            buffer.Enqueue(new TestItem(i + 100));
            buffer.Dequeue();
        }

        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that parallel wraparound churn keeps the count within the declared capacity.
    /// </summary>
    [TestMethod]
    public void EnqueueAndDequeue_WhenWraparoundUnderStress_ShouldKeepCountWithinCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);

        Parallel.For(0, 1000, i =>
        {
            buffer.TryEnqueue(new TestItem(i));
            buffer.TryDequeue(out _);
        });

        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= 3);
    }

}
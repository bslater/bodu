// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    [TestMethod]
    public void Count_AfterQuiescence_ShouldEqualToArrayLength()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10, allowOverwrite: true);

        // Concurrent churn
        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    buffer.TryEnqueue(new TestItem(i));
                    Thread.SpinWait(10);
                }
            },
            () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    buffer.TryDequeue(out _);
                    Thread.SpinWait(10);
                }
            }
        );

        // Let concurrent activity settle, then compare
        Thread.Sleep(25);
        var arr = buffer.ToArray();
        Assert.AreEqual(arr.Length, buffer.Count, "Count should match snapshot length once quiescent.");
    }

    [TestMethod]
    public void Count_WhenAllowOverwriteTogglesUnderLoad_ShouldRemainWithinBounds()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(7, allowOverwrite: false);
        var cts = new CancellationTokenSource();
        int violations = 0;

        var writer = Task.Run(() =>
        {
            int i = 0;
            while (!cts.IsCancellationRequested)
            {
                buffer.TryEnqueue(new TestItem(i++));
                Thread.SpinWait(20);
            }
        });

        var toggler = Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                buffer.AllowOverwrite = (i % 2 == 0);
                var count = buffer.Count;
                if (count < 0 || count > buffer.Capacity) Interlocked.Increment(ref violations);
                Thread.SpinWait(40);
            }
            cts.Cancel();
        });

        Task.WaitAll(writer, toggler);
        Assert.AreEqual(0, violations, "Count should always remain within [0, Capacity].");
    }

    [TestMethod]
    public void Count_WhenBufferIsCleared_ShouldBeZero()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Clear();
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void Count_WhenBufferIsEmpty_ShouldBeZero()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void Count_WhenBufferJustConstructed_ShouldBeZeroRegardlessOfCapacity()
    {
        foreach (var cap in new[] { MinCapacity, 3, 8, 16, 64 })
        {
            var buffer = new ConcurrentCircularBuffer<TestItem>(cap);
            Assert.AreEqual(0, buffer.Count, $"New buffer with capacity {cap} should have Count = 0.");
        }
    }

    [TestMethod]
    public void Count_WhenClearedDuringMutation_ShouldEventuallyResetWithinRange()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                buffer.Clear();
                Thread.SpinWait(100);
            }
        });

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                buffer.TryDequeue(out _);
            }
        });

        Task.WaitAll(clearer, writer);
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    [TestMethod]
    public void Count_WhenDequeueUntilEmpty_ShouldReachZero()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));
        while (buffer.TryDequeue(out _)) { /* drain */ }
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void Count_WhenDequeuingConcurrently_ShouldNeverBeNegative()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        Parallel.For(0, 10, i => buffer.TryDequeue(out _));

        Assert.IsTrue(buffer.Count >= 0, "Count became negative.");
    }

    [TestMethod]
    public void Count_WhenEnqueueingConcurrently_ShouldNotExceedCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        Parallel.For(0, 1000, i =>
        {
            try { buffer.TryEnqueue(new TestItem(i)); } catch { }
        });

        Assert.IsTrue(buffer.Count <= buffer.Capacity, "Count exceeded capacity.");
    }

    [TestMethod]
    public void Count_WhenItemsAreEnqueuedAndDequeued_ShouldUpdateCorrectly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.TryDequeue(out _);
        Assert.AreEqual(1, buffer.Count);
    }

    [TestMethod]
    public void Count_WhenItemsAreEnqueued_ShouldMatchItemCount()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        Assert.AreEqual(2, buffer.Count);
    }

    [TestMethod]
    public void Count_WhenMutatingConcurrently_ShouldAlwaysRemainInRange()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        for (int i = 0; i < 4; i++) buffer.Enqueue(new TestItem(i));

        var failed = false;

        var worker = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                buffer.TryDequeue(out _);
                int count = buffer.Count;
                if (count < 0 || count > buffer.Capacity) failed = true;
            }
        });

        worker.Wait();
        Assert.IsFalse(failed, "Count went out of bounds during concurrent mutation.");
    }

    [TestMethod]
    public void Count_WhenOverwriteEnabledAndBufferFull_ShouldRemainAtCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Overwrite oldest repeatedly; count should never exceed capacity
        for (int i = 4; i <= 20; i++)
            buffer.Enqueue(new TestItem(i));

        Assert.AreEqual(3, buffer.Count);
        Assert.IsTrue(buffer.Count <= buffer.Capacity);
    }

    [TestMethod]
    public void Count_WhenWrapAroundOccurs_ShouldTrackAcrossBoundary()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Cause wrap: dequeue one, enqueue one
        buffer.TryDequeue(out _);           // count -> 2
        buffer.Enqueue(new TestItem(4));    // count -> 3 (wrapped)

        Assert.AreEqual(3, buffer.Count);
    }
}
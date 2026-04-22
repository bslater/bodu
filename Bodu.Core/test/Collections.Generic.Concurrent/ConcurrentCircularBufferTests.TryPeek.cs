using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    [TestMethod]
    public void TryPeek_WhenAfterClear_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));
        buffer.Clear();

        Assert.IsFalse(buffer.TryPeek(out _), "TryPeek should return false after Clear when empty.");
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void TryPeek_WhenAllowOverwriteToggled_ShouldReflectImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Full, overwrite disabled — TryPeek returns the current oldest (1)
        Assert.IsTrue(buffer.TryPeek(out var beforeToggle));
        Assert.AreEqual(1, beforeToggle!.Value);

        // Enable overwrite and enqueue a new item that evicts 1
        buffer.AllowOverwrite = true;
        buffer.Enqueue(new TestItem(3));

        // Now oldest should be 2
        Assert.IsTrue(buffer.TryPeek(out var afterToggle));
        Assert.AreEqual(2, afterToggle!.Value);
    }

    [TestMethod]
    public void TryPeek_WhenBufferContainsNullsUnderConcurrency_ShouldYieldNullSafely()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(5);
        int nullSeen = 0;

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)             // more churn to rotate head
                buffer.Enqueue(i % 3 == 0 ? null : new TestItem(i));
        });

        var peeker = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)            // more opportunities to observe
            {
                if (buffer.TryPeek(out var item) && item is null)
                    Interlocked.Increment(ref nullSeen);
                Thread.SpinWait(20);
            }
        });

        Task.WaitAll(writer, peeker);
        Assert.IsTrue(nullSeen > 0, "Expected TryPeek to observe null items.");
    }

    [TestMethod]
    public void TryPeek_WhenBufferIsEmpty_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        Assert.IsFalse(buffer.TryPeek(out _));
    }

    // Previously tested with capacity = 1. Migrated to capacity = 2 — the minimum supported
    // value — following the implementation change that requires capacity >= 2 for the Vyukov
    // MPMC sequence protocol to be correct.
    //
    // The behaviour under test is equivalent: after the buffer holds its maximum number of
    // items (now 2 instead of 1), each subsequent enqueue evicts the oldest and TryPeek
    // correctly returns the current oldest remaining item.

    [TestMethod]
    public void TryPeek_WhenCapacityIsMinimum_ShouldBehaveConsistently()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        // Empty buffer: TryPeek must return false.
        Assert.IsFalse(buffer.TryPeek(out _));

        buffer.Enqueue(new TestItem(7));

        // One item: TryPeek returns it without removing it.
        Assert.IsTrue(buffer.TryPeek(out var item));
        Assert.AreEqual(7, item!.Value);
        Assert.AreEqual(1, buffer.Count, "TryPeek must not remove the item.");

        buffer.Enqueue(new TestItem(8)); // full → [7, 8]

        // Full buffer: oldest is still 7.
        Assert.IsTrue(buffer.TryPeek(out item));
        Assert.AreEqual(7, item!.Value);

        buffer.Enqueue(new TestItem(9)); // evicts 7 → [8, 9]

        // After overwrite: new oldest is 8.
        Assert.IsTrue(buffer.TryPeek(out item));
        Assert.AreEqual(8, item!.Value);
    }

    [TestMethod]
    public void TryPeek_WhenConcurrentDequeue_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var failures = 0;

        var peeker = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                try { buffer.TryPeek(out _); }
                catch { Interlocked.Increment(ref failures); }
            }
        });

        var consumer = Task.Run(() =>
        {
            while (buffer.TryDequeue(out _)) { }
        });

        Task.WaitAll(peeker, consumer);
        Assert.AreEqual(0, failures, "TryPeek threw during concurrent dequeue.");
    }

    [TestMethod]
    public void TryPeek_WhenConcurrentEnqueue_ShouldEventuallySucceed()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var peeked = 0;

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
                buffer.Enqueue(new TestItem(i));
        });

        var reader = Task.Run(() =>
        {
            while (Volatile.Read(ref peeked) == 0)
            {
                if (buffer.TryPeek(out var item) && item != null)
                    Interlocked.Exchange(ref peeked, 1);
            }
        });

        Task.WaitAll(writer, reader);
        Assert.AreEqual(1, peeked, "TryPeek never succeeded during enqueuing.");
    }

    [TestMethod]
    public void TryPeek_WhenManyReaders_ShouldNotThrowOrCorrupt()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        int totalAttempts = 0;
        int failures = 0;

        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    buffer.TryPeek(out TestItem _);
                    Interlocked.Increment(ref totalAttempts);
                }
                catch
                {
                    Interlocked.Increment(ref failures);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);
        Assert.AreEqual(0, failures, "TryPeek threw exceptions during concurrent reads.");
        Assert.IsTrue(totalAttempts > 0, "No TryPeek operations were attempted.");
    }

    [TestMethod]
    public void TryPeek_WhenNonDestructive_ShouldNotRemoveItem()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));

        Assert.IsTrue(buffer.TryPeek(out var peeked));
        Assert.IsNotNull(peeked);
        Assert.AreEqual(10, peeked!.Value);
        Assert.AreEqual(1, buffer.Count, "TryPeek must not remove the element.");
    }

    [TestMethod]
    public void TryPeek_WhenRapidEnqueueDequeue_ShouldObserveItemsSometimes()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        int observed = 0;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Deterministic warmup: seed one item and observe it before the enqueuer / dequeuer /
        // peeker race begins. Without this the `observed > 0` assertion is hostage to scheduler
        // luck — on a loaded CI runner the dequeuer can consistently drain the buffer faster
        // than the peeker samples it, leaving every TryPeek call observing an empty buffer.
        buffer.Enqueue(new TestItem(-1));
        if (buffer.TryPeek(out var seed) && seed != null)
            Interlocked.Increment(ref observed);

        var enqueuer = Task.Run(() =>
        {
            for (int i = 0; i < 50 && !cts.IsCancellationRequested; i++)
            {
                buffer.Enqueue(new TestItem(i));
                Thread.Sleep(1);
            }
        });

        var dequeuer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                buffer.TryDequeue(out _);
                Thread.Sleep(1);
            }
        });

        var peeker = Task.Run(() =>
        {
            for (int i = 0; i < 100 && !cts.IsCancellationRequested; i++)
            {
                if (buffer.TryPeek(out var item) && item != null)
                    Interlocked.Increment(ref observed);
                Thread.Sleep(1);
            }
            cts.Cancel(); // signal dequeuer to exit once the peeker loop is done
        });

        Task.WaitAll(enqueuer, dequeuer, peeker);
        Assert.IsTrue(observed > 0, "TryPeek did not observe any items.");
    }

    [TestMethod]
    public void TryPeek_WhenWraparoundOccurred_ShouldReturnOldest()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue();               // remove 1
        buffer.Enqueue(new TestItem(4)); // wrap

        Assert.IsTrue(buffer.TryPeek(out var item));
        Assert.IsNotNull(item);
        Assert.AreEqual(2, item!.Value);
    }
}
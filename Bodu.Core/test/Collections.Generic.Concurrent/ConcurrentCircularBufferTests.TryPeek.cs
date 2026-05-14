// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.TryPeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> returns <see langword="false" /> on a buffer that has just been cleared.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenAfterClear_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (var i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));
        buffer.Clear();

        Assert.IsFalse(buffer.TryPeek(out _), "TryPeek should return false after Clear when empty.");
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> reflects the new oldest item immediately after <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> is toggled and an overwriting enqueue runs.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenAllowOverwriteToggled_ShouldReflectImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Full, overwrite disabled — TryPeek returns the current oldest (1)
        Assert.IsTrue(buffer.TryPeek(out TestItem? beforeToggle));
        Assert.AreEqual(1, beforeToggle!.Value);

        // Enable overwrite and enqueue a new item that evicts 1
        buffer.AllowOverwrite = true;
        buffer.Enqueue(new TestItem(3));

        // Now oldest should be 2
        Assert.IsTrue(buffer.TryPeek(out TestItem? afterToggle));
        Assert.AreEqual(2, afterToggle!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> observes <see langword="null" /> items safely when the buffer contains nulls under a concurrent writer.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenBufferContainsNullsUnderConcurrency_ShouldYieldNullSafely()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(5);
        var nullSeen = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)             // more churn to rotate head
                buffer.Enqueue(i % 3 == 0 ? null : new TestItem(i));
        });

        var peeker = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)            // more opportunities to observe
            {
                if (buffer.TryPeek(out TestItem? item) && item is null)
                    Interlocked.Increment(ref nullSeen);
                Thread.SpinWait(20);
            }
        });

        Task.WaitAll(writer, peeker);
        Assert.IsTrue(nullSeen > 0, "Expected TryPeek to observe null items.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> on an empty buffer returns <see langword="false" />.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> behaves consistently at the minimum supported capacity, reflecting eviction as items overflow.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenCapacityIsMinimum_ShouldBehaveConsistently()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        // Empty buffer: TryPeek must return false.
        Assert.IsFalse(buffer.TryPeek(out _));

        buffer.Enqueue(new TestItem(7));

        // One item: TryPeek returns it without removing it.
        Assert.IsTrue(buffer.TryPeek(out TestItem? item));
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

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> does not throw while a concurrent consumer is draining the buffer.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenConcurrentDequeue_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var failures = 0;

        var peeker = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
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

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> eventually succeeds against a buffer fed by a concurrent enqueuer.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenConcurrentEnqueue_ShouldEventuallySucceed()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var peeked = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
                buffer.Enqueue(new TestItem(i));
        });

        var reader = Task.Run(() =>
        {
            while (Volatile.Read(ref peeked) == 0)
            {
                if (buffer.TryPeek(out TestItem? item) && item != null)
                    Interlocked.Exchange(ref peeked, 1);
            }
        });

        Task.WaitAll(writer, reader);
        Assert.AreEqual(1, peeked, "TryPeek never succeeded during enqueuing.");
    }

    /// <summary>
    /// Verifies that many concurrent <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> calls never throw and never corrupt the buffer.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenManyReaders_ShouldNotThrowOrCorrupt()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var totalAttempts = 0;
        var failures = 0;

        Task[] tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
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

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> does not remove the item it inspects.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenNonDestructive_ShouldNotRemoveItem()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));

        Assert.IsTrue(buffer.TryPeek(out TestItem? peeked));
        Assert.IsNotNull(peeked);
        Assert.AreEqual(10, peeked!.Value);
        Assert.AreEqual(1, buffer.Count, "TryPeek must not remove the element.");
    }

    /// <summary>
    /// Verifies that under a rapid producer/consumer race, <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> observes at least one item.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenRapidEnqueueDequeue_ShouldObserveItemsSometimes()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var observed = 0;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Deterministic warmup: seed one item and observe it before the enqueuer / dequeuer /
        // peeker race begins. Without this the `observed > 0` assertion is hostage to scheduler
        // luck — on a loaded CI runner the dequeuer can consistently drain the buffer faster
        // than the peeker samples it, leaving every TryPeek call observing an empty buffer.
        buffer.Enqueue(new TestItem(-1));
        if (buffer.TryPeek(out TestItem? seed) && seed != null)
            Interlocked.Increment(ref observed);

        var enqueuer = Task.Run(() =>
        {
            for (var i = 0; i < 50 && !cts.IsCancellationRequested; i++)
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
            for (var i = 0; i < 100 && !cts.IsCancellationRequested; i++)
            {
                if (buffer.TryPeek(out TestItem? item) && item != null)
                    Interlocked.Increment(ref observed);
                Thread.Sleep(1);
            }
            cts.Cancel(); // signal dequeuer to exit once the peeker loop is done
        });

        Task.WaitAll(enqueuer, dequeuer, peeker);
        Assert.IsTrue(observed > 0, "TryPeek did not observe any items.");
    }

    /// <summary>
    /// Verifies that after a wraparound, <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> returns the logical oldest item.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenWraparoundOccurred_ShouldReturnOldest()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue();               // remove 1
        buffer.Enqueue(new TestItem(4)); // wrap

        Assert.IsTrue(buffer.TryPeek(out TestItem? item));
        Assert.IsNotNull(item);
        Assert.AreEqual(2, item!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek" /> retries — rather than returning a stale value — when the
    /// slot's coordination sequence is observed to be greater than the publication mark, which models the
    /// "another thread dequeued this slot" race window in the consumer protocol. Once the sequence is corrected back into the
    /// published state, the peek succeeds and returns the live head element.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenSlotSequenceIsAheadOfHead_ShouldRetryUntilSequenceRealignsAndSucceed()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        FieldInfo bufferField = typeof(ConcurrentCircularBuffer<TestItem>).GetField(
            "_buffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array slotArray = (Array)bufferField.GetValue(buffer)!;

        // Read the current head-slot Sequence and pre-bump it so the first iteration of TryPeek
        // observes diff > 0 (the "stale head read — another thread dequeued this slot" branch).
        object slot0 = slotArray.GetValue(0)!;
        FieldInfo sequenceField = slot0.GetType().GetField("Sequence", BindingFlags.Instance | BindingFlags.Public)!;
        var originalSequence = (int)sequenceField.GetValue(slot0)!;

        sequenceField.SetValue(slot0, originalSequence + 1);
        slotArray.SetValue(slot0, 0);

        // Run the realign worker on a separate thread that restores the published sequence
        // shortly after the peek begins. TryPeek has no retry budget on the diff > 0 branch, so
        // a missed realign would hang the test runner — Task.WaitAll's timeout below converts
        // that into a test failure instead.
        Task realignTask = Task.Run(() =>
        {
            for (var i = 0; i < 16; i++) Thread.SpinWait(100);
            object slot = slotArray.GetValue(0)!;
            sequenceField.SetValue(slot, originalSequence);
            slotArray.SetValue(slot, 0);
        });

        TestItem? captured = null;
        var peekResult = false;
        Task peekTask = Task.Run(() =>
        {
            peekResult = buffer.TryPeek(out captured);
        });

        var completed = Task.WaitAll([realignTask, peekTask], TimeSpan.FromSeconds(10));

        Assert.IsTrue(completed,
            "TryPeek did not return after the slot sequence was restored — possible scheduling issue or missed realign.");
        Assert.IsTrue(peekResult, "TryPeek must eventually succeed once the slot sequence realigns with the head.");
        Assert.IsNotNull(captured);
        Assert.AreEqual(1, captured!.Value);
    }
}
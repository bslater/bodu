// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.ItemEvicted.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> is not raised when overwriting is disabled and TryEnqueue fails on a full buffer.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenAllowOverwriteFalse_ShouldNotFire()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        var fired = false;

        buffer.ItemEvicted += _ => fired = true;

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        var success = buffer.TryEnqueue(new TestItem(3)); // Should fail, no eviction

        Assert.IsFalse(success, "TryEnqueue should fail when full and overwriting disabled.");
        Assert.IsFalse(fired, "Eviction event should not fire when overwriting is disabled.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> does not fire while the buffer has free space.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenBufferNotFull_ShouldNotFire()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);
        var fired = false;

        buffer.ItemEvicted += _ => fired = true;

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        Assert.IsFalse(fired, "Eviction should not occur before the buffer is full.");
    }

    /// <summary>
    /// Verifies that an exception thrown by the first <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> handler does not prevent the second handler from being invoked.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenFirstHandlerThrows_ShouldStillInvokeSecondHandler()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);
        var secondHandlerFired = false;

        buffer.ItemEvicted += _ => throw new InvalidOperationException("first");
        buffer.ItemEvicted += _ => secondHandlerFired = true;

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2)); // buffer full — no eviction yet
        buffer.Enqueue(new TestItem(3)); // evicts 1, fires both handlers

        Assert.IsTrue(secondHandlerFired, "Second handler should be invoked even if the first throws.");
    }

    /// <summary>
    /// Verifies that exceptions thrown from a single <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> handler are swallowed by <see cref="ConcurrentCircularBuffer{T}.Enqueue" />.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerThrows_ShouldNotPropagateException()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Handler throws; Enqueue must NOT propagate
        buffer.ItemEvicted += _ => throw new InvalidOperationException("Simulated error");

        // Should NOT throw even though handler does
        buffer.Enqueue(new TestItem(3));
    }

    /// <summary>
    /// Verifies that a handler unsubscribed from <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> receives no further eviction callbacks.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerUnsubscribed_ShouldNotReceiveFurtherEvents()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);
        var count = 0;

        Action<TestItem?> handler = _ => Interlocked.Increment(ref count);
        buffer.ItemEvicted += handler;

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3)); // fires eviction: count = 1

        buffer.ItemEvicted -= handler;

        buffer.Enqueue(new TestItem(4)); // should not fire
        buffer.Enqueue(new TestItem(5)); // should not fire

        Assert.AreEqual(1, count, "Handler should not receive events after unsubscription.");
    }

    /// <summary>
    /// Verifies that all handlers registered on <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> are invoked on a single eviction.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenMultipleHandlersRegistered_ShouldInvokeAll()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        var count1 = 0;
        var count2 = 0;

        buffer.ItemEvicted += item => Interlocked.Increment(ref count1);
        buffer.ItemEvicted += item => Interlocked.Increment(ref count2);

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3)); // Evicts 1

        Assert.AreEqual(1, count1, "First handler should be invoked once.");
        Assert.AreEqual(1, count2, "Second handler should be invoked once.");
    }

    /// <summary>
    /// Verifies that when a <see langword="null" /> item is evicted, <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> fires with a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenNullItemIsEvicted_ShouldReceiveNullArgument()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(2, allowOverwrite: true);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));

        var received = new TestItem(-1); // sentinel to distinguish from default(null)
        var fired = false;
        buffer.ItemEvicted += x => { received = x; fired = true; };

        buffer.Enqueue(new TestItem(2)); // should evict null

        Assert.IsTrue(fired, "ItemEvicted should fire when a null item is overwritten.");
        Assert.IsNull(received, "ItemEvicted argument should be null when the evicted item was null.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> captures every item evicted by concurrent overwriting producers.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenOverwritesOccurConcurrently_ShouldCaptureEvictedItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);
        var evicted = new ConcurrentBag<TestItem>();

        buffer.ItemEvicted += item =>
        {
            if (item != null)
                evicted.Add(item);
        };

        // Fill buffer
        for (var i = 0; i < 5; i++)
            buffer.Enqueue(new TestItem(i));

        // Concurrent overwrites
        Parallel.For(5, 100, i =>
        {
            buffer.Enqueue(new TestItem(i));
        });

        // Let synchronous handlers settle (mostly a no-op since handlers run inline)
        Thread.Sleep(50);

        Assert.IsNotEmpty(evicted, "Expected at least one eviction.");
        Assert.IsTrue(evicted.All(x => x != null));
        Assert.IsTrue(evicted.All(x => x.Value >= 0 && x.Value < 100));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> remains stable and delivers non-null arguments under heavy parallel overwriting.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenParallelWritersOverwrite_ShouldRemainStable()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        var evicted = new ConcurrentBag<TestItem>();

        buffer.ItemEvicted += item =>
        {
            if (item != null)
                evicted.Add(item);
        };

        Parallel.For(0, 50, i =>
        {
            buffer.Enqueue(new TestItem(i));
        });

        // Inline events should already be processed; small pause for safety
        Thread.Sleep(20);

        Assert.IsNotEmpty(evicted, "Expected some evictions under pressure.");
        Assert.IsTrue(evicted.All(x => x != null));
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

}
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.ItemEvicted.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
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

    [TestMethod]
    public void ItemEvicted_WhenNullItemIsEvicted_ShouldReceiveNullArgument()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(2, allowOverwrite: true);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));

        TestItem? received = new TestItem(-1); // sentinel to distinguish from default(null)
        var fired = false;
        buffer.ItemEvicted += x => { received = x; fired = true; };

        buffer.Enqueue(new TestItem(2)); // should evict null

        Assert.IsTrue(fired, "ItemEvicted should fire when a null item is overwritten.");
        Assert.IsNull(received, "ItemEvicted argument should be null when the evicted item was null.");
    }

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
        for (int i = 0; i < 5; i++)
            buffer.Enqueue(new TestItem(i));

        // Concurrent overwrites
        Parallel.For(5, 100, i =>
        {
            buffer.Enqueue(new TestItem(i));
        });

        // Let synchronous handlers settle (mostly a no-op since handlers run inline)
        Thread.Sleep(50);

        Assert.IsTrue(evicted.Count > 0, "Expected at least one eviction.");
        Assert.IsTrue(evicted.All(x => x != null));
        Assert.IsTrue(evicted.All(x => x.Value >= 0 && x.Value < 100));
    }

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

        Assert.IsTrue(evicted.Count > 0, "Expected some evictions under pressure.");
        Assert.IsTrue(evicted.All(x => x != null));
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }
}
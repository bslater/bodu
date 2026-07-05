// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.TryEnqueue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> reflects the current <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> value immediately after it is toggled.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenAllowOverwriteToggled_ShouldReflectImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Overwrite disabled → should fail (return false)
        Assert.IsFalse(buffer.TryEnqueue(new TestItem(3)));

        // Enable overwrite → should succeed and evict 1
        buffer.AllowOverwrite = true;
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(4)));
        CollectionAssert.AreEqual(new[] { 2, 4 }, buffer.ToArray().Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> on a full buffer with overwriting enabled returns <see langword="true" />, evicts the oldest item, and fires <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" />.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenBufferFullAndAllowOverwriteTrue_ShouldReturnTrueAndEvict()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        TestItem? evicted = null;
        buffer.ItemEvicted += x => evicted = x;

        bool ok = buffer.TryEnqueue(new TestItem(3));

        Assert.IsTrue(ok, "TryEnqueue should return true in overwrite mode.");
        Assert.IsNotNull(evicted, "ItemEvicted should fire when the oldest item is displaced.");
        Assert.AreEqual(1, evicted!.Value);
        AssertBufferContainsExactlyValues(buffer, 2, 3);
    }

    /// <summary>
    /// Verifies that at the minimum capacity with overwriting enabled, <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> always succeeds.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenCapacityIsMinAndOverwriteEnabled_ShouldAlwaysReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);

        for (int i = 0; i < 1000; i++)
            Assert.IsTrue(buffer.TryEnqueue(new TestItem(i)));

        Assert.IsTrue(buffer.Count is >= 0 and <= MinCapacity);
    }

    /// <summary>
    /// Verifies that with overwriting disabled, concurrent producers together succeed exactly <c>Capacity</c> times and the buffer fills to capacity.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenConcurrentProducersNoOverwrite_ShouldAcceptUpToCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(100, allowOverwrite: false);
        int successes = 0;

        Parallel.For(0, 150, i =>
        {
            if (buffer.TryEnqueue(new TestItem(i)))
                Interlocked.Increment(ref successes);
        });

        Assert.AreEqual(100, successes);
        Assert.AreEqual(100, buffer.Count);
    }

    /// <summary>
    /// Verifies that concurrent producers with overwriting enabled never let <see cref="ConcurrentCircularBuffer{T}.Count" /> exceed <see cref="ConcurrentCircularBuffer{T}.Capacity" /> and every stored item is non-null.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenConcurrentProducersOverwriteEnabled_ShouldNeverExceedCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10, allowOverwrite: true);

        Parallel.For(0, 50, i =>
        {
            buffer.TryEnqueue(new TestItem(i));
        });

        Assert.IsLessThanOrEqualTo(buffer.Capacity, buffer.Count, $"Expected Count <= {buffer.Capacity}, got {buffer.Count}.");

        TestItem[] snapshot = buffer.ToArray();
        Assert.IsLessThanOrEqualTo(buffer.Capacity, snapshot.Length, $"ToArray exceeded capacity: {snapshot.Length}");
        Assert.IsTrue(snapshot.All(x => x is not null), "All items in buffer should be non-null.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> returns <see langword="false" /> when the buffer is full and overwriting is disabled.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenFullAndOverwriteDisabled_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        bool ok = buffer.TryEnqueue(new TestItem(3));
        Assert.IsFalse(ok, "TryEnqueue must return false when full and overwrite disabled.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> returns <see langword="true" /> on a full buffer with overwriting enabled and keeps the count pinned at capacity.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenFullAndOverwriteEnabled_ShouldReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(1)));
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(2)));
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(3))); // Overwrites 1
        Assert.AreEqual(2, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> never throws under heavy multi-threaded contention and respects capacity.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenMultipleThreads_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(25);
        var exceptions = new ConcurrentBag<Exception>();

        Task[] tasks = Enumerable.Range(0, 5).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    buffer.TryEnqueue(new TestItem(i + t * 10));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.IsEmpty(exceptions);
        Assert.IsLessThanOrEqualTo(buffer.Capacity, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> accepts a <see langword="null" /> reference, stores it, and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenNullReferenceProvided_ShouldStoreNullAndReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<string>(3, allowOverwrite: false);

        Assert.IsTrue(buffer.TryEnqueue(null));
        Assert.IsTrue(buffer.TryEnqueue("X"));

        string[] snapshot = buffer.ToArray();
        CollectionAssert.AreEqual(new[] { (string?)null, "X" }, snapshot);
    }

    /// <summary>
    /// Verifies that with overwriting disabled, every concurrent <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> on a full buffer returns <see langword="false" /> and leaves the contents unchanged.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenOverwriteDisabledAndFull_ShouldReturnFalseUnderContention()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4, allowOverwrite: false);
        for (int i = 0; i < 4; i++) buffer.Enqueue(new TestItem(i));

        int successes = 0, failures = 0;

        Parallel.For(0, 64, i =>
        {
            if (buffer.TryEnqueue(new TestItem(100 + i)))
                Interlocked.Increment(ref successes);
            else
                Interlocked.Increment(ref failures);
        });

        Assert.AreEqual(0, successes, "No TryEnqueue should succeed when full and overwrite disabled.");
        Assert.IsGreaterThan(0, failures, "Expected at least one TryEnqueue to return false under contention.");
        Assert.AreEqual(4, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> returns <see langword="true" /> when the buffer has free space and updates the count accordingly.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenSpaceAvailable_ShouldReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: false);
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(1)));
        Assert.IsTrue(buffer.TryEnqueue(new TestItem(2)));
        Assert.AreEqual(2, buffer.Count);
    }

}
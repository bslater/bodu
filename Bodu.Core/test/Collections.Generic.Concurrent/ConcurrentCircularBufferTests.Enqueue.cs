// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Enqueue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that after <see cref="ConcurrentCircularBuffer{T}.Clear" />, subsequent enqueues start from an empty state and populate the buffer in FIFO order.
    /// </summary>
    [TestMethod]
    public void Enqueue_AfterClear_ShouldStartFromEmptyAndAcceptNewItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Clear();

        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(11));

        Assert.AreEqual(2, buffer.Count);
        AssertBufferContainsExactlyValues(buffer, 10, 11);
    }

    /// <summary>
    /// Verifies that when <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> is toggled concurrently with
    /// enqueueing into a full buffer, at least one enqueue throws <see cref="InvalidOperationException" /> while
    /// overwriting is disabled, and the buffer never exceeds its configured capacity.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenAllowOverwriteToggledDuringEnqueue_ShouldAlternateBetweenThrowsAndEvictions()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var exceptions = new ConcurrentBag<Exception>();

        // Handshake: the toggler confirms AllowOverwrite = false before the writer's first
        // Enqueue, guaranteeing at least one deterministic InvalidOperationException against
        // the full buffer. Concurrent toggling then proceeds for the remainder of both loops.
        using var togglerPrimed = new ManualResetEventSlim(initialState: false);

        var writer = Task.Run(() =>
        {
            togglerPrimed.Wait();

            for (var i = 0; i < 200; i++)
            {
                try { buffer.Enqueue(new TestItem(100 + i)); }
                catch (Exception ex) { exceptions.Add(ex); }
                Thread.SpinWait(30);
            }
        });

        var toggler = Task.Run(() =>
        {
            buffer.AllowOverwrite = false;
            togglerPrimed.Set();

            // Continue flipping starting from i = 1 so the next write remains false,
            // preserving the original "true on i % 3 == 0" cadence after the primed state.
            for (var i = 1; i < 200; i++)
            {
                buffer.AllowOverwrite = (i % 3 == 0);
                Thread.SpinWait(50);
            }
        });

        Task.WaitAll(writer, toggler);

        // We expect a mix: some throws (when flag false and full), some evictions (when flag true).
        Assert.IsNotEmpty(exceptions, "Expected some InvalidOperationExceptions while overwrite was disabled.");
        Assert.IsLessThanOrEqualTo(buffer.Capacity, buffer.Count);
    }

    /// <summary>
    /// Verifies that with overwriting disabled, concurrent enqueues against a full buffer all throw <see cref="InvalidOperationException" /> and the buffer's contents remain unchanged.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenBufferFullAndAllowOverwriteFalse_ShouldBlockOverwrites()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var failures = new ConcurrentBag<Exception>();

        Parallel.For(0, 10, i =>
        {
            try { buffer.Enqueue(new TestItem(100 + i)); }
            catch (InvalidOperationException ex) { failures.Add(ex); }
        });

        Assert.HasCount(10, failures);
        Assert.AreEqual(3, buffer.Count);
        AssertBufferContainsExactlyValues(buffer, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that enqueueing into a full buffer with overwriting disabled throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenBufferFullAndAllowOverwriteFalse_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(new TestItem(3));
        });
    }

    /// <summary>
    /// Verifies that overwriting a full buffer evicts the oldest item and raises <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> in eviction order.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenBufferFullAndAllowOverwriteTrue_ShouldEvictOldestAndRaiseEvents()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        var evicted = new List<int>();

        buffer.ItemEvicted += x => { if (x != null) evicted.Add(x.Value); };

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3)); // evicts 1
        buffer.Enqueue(new TestItem(4)); // evicts 2

        CollectionAssert.AreEqual(new[] { 1, 2 }, evicted.ToArray(), "ItemEvicted should report items in eviction order.");

        TestItem[] snapshot = buffer.ToArray();
        CollectionAssert.AreEqual(new[] { 3, 4 }, snapshot.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that concurrent enqueues into a full buffer with overwriting enabled evict older entries so only the most recent values are retained.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenBufferFullAndAllowOverwriteTrue_ShouldEvictOldestItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        Parallel.For(0, 10, i => buffer.Enqueue(new TestItem(100 + i)));

        AssertBufferContainsOnlyValuesInRange(buffer, expectedCount: 3, minInclusive: 100, maxInclusive: 109);
    }

    /// <summary>
    /// Verifies that at minimum capacity with overwriting enabled, the buffer always retains the most recently enqueued items.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenCapacityIsMinAndOverwriteTrue_ShouldAlwaysKeepMostRecent()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 3 }, values);
    }

    /// <summary>
    /// Verifies that slots freed by a concurrent dequeuer are safely reused by enqueuers without corruption or capacity violation.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenConcurrentDequeueFreesSlots_ShouldReuseSlotsSafely()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (var i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var enqueueSuccess = new ConcurrentBag<bool>();

        var writer = Task.Run(() =>
        {
            for (var i = 100; i < 200; i++)
            {
                var success = buffer.TryEnqueue(new TestItem(i));
                enqueueSuccess.Add(success);
                Thread.SpinWait(5);
            }
        });

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                buffer.TryDequeue(out _);
                Thread.SpinWait(10);
            }
        });

        Task.WaitAll(writer, reader);

        Assert.Contains(s => s, enqueueSuccess, "No enqueue succeeded during concurrent dequeue.");
        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity);
    }

    /// <summary>
    /// Verifies that concurrent enqueues of duplicate values all land in the buffer and none are silently discarded.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenDuplicateValuesProvidedConcurrently_ShouldStoreAll()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10, allowOverwrite: true);

        Parallel.For(0, 10, _ => buffer.Enqueue(new TestItem(5)));

        Assert.AreEqual(10, buffer.Count);
        Assert.IsTrue(buffer.ToArray().All(x => x?.Value == 5));
    }

    /// <summary>
    /// Verifies that an exception thrown by an <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> handler is swallowed and the enqueue still completes successfully.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenItemEvictedHandlerThrows_ShouldSwallowExceptionAndCompleteEnqueue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Track calls to ensure the handler ran.
        var evictedCalls = 0;
        buffer.ItemEvicted += _ =>
        {
            Interlocked.Increment(ref evictedCalls);
            throw new InvalidOperationException("boom-evicted");
        };

        // Enqueue must NOT throw; exception from ItemEvicted is swallowed.
        try
        {
            buffer.Enqueue(new TestItem(3));
        }
        catch (Exception ex)
        {
            Assert.Fail($"Enqueue should swallow exceptions from ItemEvicted, but threw: {ex}");
        }

        // Handler should have been invoked once (for evicting "1").
        Assert.AreEqual(1, evictedCalls, "ItemEvicted should be invoked exactly once for the eviction.");

        // Buffer should contain the newest two items in FIFO order: [2, 3].
        var snapshot = buffer.ToArray().Select(x => x?.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 3 }, snapshot, "Enqueue should complete successfully when ItemEvicted throws.");
    }

    /// <summary>
    /// Verifies that enqueueing a sequence of <see langword="null" /> and non-null values preserves their exact insertion order.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenMultipleNullsAndSpaceAvailable_ShouldRetainOrder()
    {
        var buffer = new ConcurrentCircularBuffer<string>(3);
        buffer.Enqueue(null);
        buffer.Enqueue("X");
        buffer.Enqueue(null);

        var snapshot = buffer.ToArray();
        CollectionAssert.AreEqual(new[] { null, "X", null }, snapshot);
    }

    /// <summary>
    /// Verifies that concurrent enqueues into a buffer with ample capacity store every item without loss.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenMultipleThreadsHaveSpace_ShouldStoreAllItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(100, allowOverwrite: false);

        Parallel.For(0, 100, i => buffer.TryEnqueue(new TestItem(i)));

        Assert.AreEqual(100, buffer.Count);
        Assert.IsTrue(buffer.ToArray().All(x => x != null));
    }

    /// <summary>
    /// Verifies that <see langword="null" /> items are accepted by concurrent enqueues and appear in the buffer snapshot.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenNullsProvidedConcurrently_ShouldAcceptNulls()
    {
        var buffer = new ConcurrentCircularBuffer<string>(10);

        Parallel.For(0, 10, i =>
        {
            buffer.Enqueue(i % 2 == 0 ? null : $"Value-{i}");
        });

        var items = buffer.ToArray();
        Assert.HasCount(10, items);
        Assert.Contains(x => x == null, items);
    }

    /// <summary>
    /// Verifies that concurrent enqueues that cause the buffer to wrap retain only items from the enqueued value range at full capacity.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenWraparoundOccursConcurrently_ShouldRetainOnlyMostRecentItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);

        Parallel.For(0, 10, i => buffer.Enqueue(new TestItem(i)));

        // All retained items must come from the enqueued range; the exact three survivors
        // depend on scheduling, but each must be a valid value and the count must be 3.
        AssertBufferContainsOnlyValuesInRange(buffer, expectedCount: 3, minInclusive: 0, maxInclusive: 9);

        // Monotonic ordering cannot be asserted here. Parallel.For enqueues in thread-scheduling
        // order, not value order — thread 9 can legitimately run before thread 0, producing a
        // snapshot such as [9, 0, 1] that is internally correct FIFO but not value-sorted.
        // The range-and-count check above is the only valid invariant for concurrent enqueues
        // where item values and insertion order are independent.
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryEnqueue" /> returns <see langword="false" /> on a full buffer with overwriting disabled and leaves the buffer unchanged.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenBufferFullAndAllowOverwriteFalse_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var ok = buffer.TryEnqueue(new TestItem(3));
        Assert.IsFalse(ok, "TryEnqueue should return false when full and overwriting is disabled.");
        AssertBufferContainsExactlyValues(buffer, 1, 2);
    }

}
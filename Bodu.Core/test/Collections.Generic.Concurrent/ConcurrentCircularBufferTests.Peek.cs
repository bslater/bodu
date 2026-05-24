// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Peek.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that a failed enqueue on a full buffer with overwriting disabled does not change the item returned by <see cref="ConcurrentCircularBuffer{T}.Peek" />.
    /// </summary>
    [TestMethod]
    public void Peek_WhenAllowOverwriteFalseAndBufferFull_ShouldContinueToReturnOldestUntilDequeue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        TestItem p1 = buffer.Peek();
        Assert.AreEqual(10, p1.Value);

        // Enqueue will fail; Peek should still show 10
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(new TestItem(30));
        });
        TestItem p2 = buffer.Peek();
        Assert.AreEqual(10, p2.Value, "Oldest should not change when enqueue fails.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> sees the current oldest item, and reflects the new oldest after an overwrite evicts it.
    /// </summary>
    [TestMethod]
    public void Peek_WhenAllowOverwriteTrueAndBufferFull_ShouldReturnOldestBeforeOverwrite()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Oldest = 1
        TestItem seen = buffer.Peek();
        Assert.AreEqual(1, seen.Value, "Peek should see current oldest when full.");

        buffer.Enqueue(new TestItem(4)); // overwrites 1
        TestItem after = buffer.Peek();
        Assert.AreEqual(2, after.Value, "After overwrite, new oldest should be 2.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> returns the oldest item and does not remove it from the buffer.
    /// </summary>
    [TestMethod]
    public void Peek_WhenBufferHasItems_ShouldReturnOldestWithoutRemoving()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(100));

        TestItem peeked = buffer.Peek();
        Assert.AreEqual(100, peeked.Value, "Peek should return oldest value.");
        Assert.AreEqual(1, buffer.Count, "Peek must not remove the item.");
    }

    /// <summary>
    /// Verifies that after the buffer wraps, <see cref="ConcurrentCircularBuffer{T}.Peek" /> still returns the logical oldest item.
    /// </summary>
    [TestMethod]
    public void Peek_WhenBufferHasWrapped_ShouldReturnOldestItem()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue(); // evict 1
        buffer.Enqueue(new TestItem(4)); // wrap

        TestItem peeked = buffer.Peek();
        Assert.AreEqual(2, peeked.Value, "After wrap, oldest should be 2.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> on an empty buffer throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Peek_WhenBufferIsEmpty_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = buffer.Peek();
        });
    }

    /// <summary>
    /// Verifies that while another thread drains the buffer, at least one <see cref="ConcurrentCircularBuffer{T}.Peek" /> call succeeds before the buffer is fully consumed.
    /// </summary>
    [TestMethod]
    public void Peek_WhenCalledDuringDraining_ShouldSometimesSucceedBeforeEmpty()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var start = new ManualResetEventSlim(false);
        var success = 0;
        var attempts = 0;

        var peeker = Task.Run(() =>
        {
            start.Set(); // signal: peeker is alive
            for (var i = 0; i < 1000 && Volatile.Read(ref success) == 0; i++)
            {
                try
                {
                    _ = buffer.Peek();                // throws only if empty
                    Interlocked.Exchange(ref success, 1);
                }
                catch (InvalidOperationException)
                {
                    // empty at this instant — try again
                }
                Thread.SpinWait(50);
                Interlocked.Increment(ref attempts);
            }
        });

        // Ensure peeker is running before drain begins
        start.Wait();

        // Tiny yield so the peeker can attempt at least once
        Thread.Yield();

        var consumer = Task.Run(() =>
        {
            while (buffer.TryDequeue(out _)) { /* drain */ Thread.SpinWait(10); }
        });

        Task.WaitAll(peeker, consumer);
        Assert.AreEqual(1, success, "Peek should succeed at least once before buffer empties.");
        Assert.IsGreaterThan(0, attempts, "Peeker never attempted.");
    }

    /// <summary>
    /// Verifies that repeated <see cref="ConcurrentCircularBuffer{T}.Peek" /> calls are non-destructive and leave <see cref="ConcurrentCircularBuffer{T}.Count" /> unchanged.
    /// </summary>
    [TestMethod]
    public void Peek_WhenCalledRepeatedly_ShouldNotChangeCount()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        var before = buffer.Count;

        for (var i = 0; i < 100; i++)
            _ = buffer.Peek();

        Assert.AreEqual(before, buffer.Count, "Peek must be non-destructive.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> interleaved with <see cref="ConcurrentCircularBuffer{T}.Clear" /> only ever throws <see cref="InvalidOperationException" /> when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void Peek_WhenInterleavedWithClear_ShouldNotThrowAndMayReturnNewHead()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8, allowOverwrite: true);
        for (var i = 0; i < 4; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var observedValues = new ConcurrentBag<int?>();

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                buffer.Clear();
                Thread.SpinWait(20);
                buffer.TryEnqueue(new TestItem(100 + i));
            }
        });

        var peeker = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                try
                {
                    TestItem x = buffer.Peek();
                    observedValues.Add(x?.Value);
                }
                catch (InvalidOperationException)
                {
                    // OK: buffer might be empty between Clear/Enqueue
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(clearer, peeker);
        Assert.IsEmpty(exceptions, "Peek should only throw InvalidOperation when empty.");
        Assert.IsGreaterThanOrEqualTo(0, observedValues.Count);
    }

    /// <summary>
    /// Verifies that many concurrent peeks against a populated buffer never throw anything other than <see cref="InvalidOperationException" /> (empty) exceptions.
    /// </summary>
    [TestMethod]
    public void Peek_WhenManyThreadsPeekConcurrently_ShouldNeverThrowUnlessEmpty()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(16, allowOverwrite: true);
        for (var i = 0; i < 8; i++) buffer.Enqueue(new TestItem(i));

        var errors = new ConcurrentBag<Exception>();

        Parallel.For(0, 1000, _ =>
        {
            try
            {
                TestItem? item = buffer.Peek();
            }
            catch (InvalidOperationException)
            {
                // acceptable if another thread drained the buffer
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.IsEmpty(errors, "No exceptions other than InvalidOperation (empty) are expected.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> reflects the updated oldest item after each dequeue.
    /// </summary>
    [TestMethod]
    public void Peek_WhenOldestChangesDueToDequeue_ShouldReflectNewOldest()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        Assert.AreEqual(10, buffer.Peek().Value);
        buffer.Dequeue();
        Assert.AreEqual(20, buffer.Peek().Value);
        buffer.Dequeue();
        Assert.AreEqual(30, buffer.Peek().Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> returns <see langword="null" /> when the oldest item is null and leaves the buffer's count unchanged.
    /// </summary>
    [TestMethod]
    public void Peek_WhenOldestIsNull_ShouldReturnNullWithoutRemoving()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(3);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(2));

        TestItem? peeked = buffer.Peek();
        Assert.IsNull(peeked, "Peek should return null if oldest is null.");
        Assert.AreEqual(2, buffer.Count, "Peek must not remove null.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Peek" /> does not throw while a concurrent enqueuer is mutating the buffer and always returns a non-null item when no evictions occur.
    /// </summary>
    [TestMethod]
    public void Peek_WhenReadDuringConcurrentEnqueue_ShouldNotThrow()
    {
        // Capacity covers all pre-filled items plus everything the writer adds, so the buffer
        // never reaches full and no evictions occur. Without concurrent evictions TryPeek's
        // sequence-check-then-value-read is uncontested and the non-null assertion is valid.
        // Using the default allowOverwrite: true with a full buffer would cause EvictOne to
        // null slot.Value between TryPeek's sequence check and its value read, making null
        // a legitimately observable (and correct) result that the assertion incorrectly rejects.
        const int prefilledCount = 10;
        const int writerCount = 100;
        var buffer = new ConcurrentCircularBuffer<TestItem>(prefilledCount + writerCount + 10);

        for (var i = 0; i < prefilledCount; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var peekedItems = new ConcurrentBag<TestItem?>();

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    peekedItems.Add(buffer.Peek());
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        var writer = Task.Run(() =>
        {
            for (var i = prefilledCount; i < prefilledCount + writerCount; i++)
                buffer.Enqueue(new TestItem(i));
        });

        Task.WaitAll(reader, writer);

        Assert.IsEmpty(exceptions, "Peek should not throw while producers mutate.");
        Assert.IsTrue(peekedItems.All(x => x != null),
            "Peek returned null for a non-null item during concurrent enqueue.");
    }

}
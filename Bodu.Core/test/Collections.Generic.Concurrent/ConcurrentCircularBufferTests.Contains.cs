// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Contains.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> returns <see langword="false" /> for items that were present before <see cref="ConcurrentCircularBuffer{T}.Clear" /> was called.
    /// </summary>
    [TestMethod]
    public void Contains_AfterClear_ShouldReturnFalseForPreviouslyPresentItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var item = new TestItem(5);

        buffer.Enqueue(item);
        Assert.IsTrue(buffer.Contains(item));

        buffer.Clear();
        Assert.IsFalse(buffer.Contains(item));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> uses default equality consistently across concurrent readers.
    /// </summary>
    [TestMethod]
    public void Contains_WhenAccessedConcurrently_ShouldHonorDefaultEquality()
    {
        var buffer = new ConcurrentCircularBuffer<string>(10);
        for (var i = 0; i < 5; i++) buffer.Enqueue("Test" + i);

        var result = Enumerable.Range(0, 100).AsParallel().All(_ =>
            buffer.Contains("Test2") && !buffer.Contains("Missing"));

        Assert.IsTrue(result, "Contains failed to honor equality under concurrency.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> returns <see langword="true" /> when a <see langword="null" /> item is present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenBufferContainsNull_ShouldReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<object>(5);
        buffer.Enqueue(null);
        Assert.IsTrue(buffer.Contains(null));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> returns <see langword="false" /> for any item when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void Contains_WhenBufferIsEmpty_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        Assert.IsFalse(buffer.Contains(new TestItem(1)));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> does not throw while enqueues and clears run concurrently.
    /// </summary>
    [TestMethod]
    public void Contains_WhenConcurrentEnqueueAndClear_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        var target = new TestItem(42);
        var exceptions = new ConcurrentBag<Exception>();

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                buffer.TryEnqueue(i % 2 == 0 ? target : new TestItem(i));
                if (i % 10 == 0) buffer.Clear();
                Thread.SpinWait(20);
            }
        });

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                try
                {
                    _ = buffer.Contains(target);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                Thread.SpinWait(10);
            }
        });

        Task.WaitAll(writer, reader);

        Assert.IsEmpty(exceptions, "Contains threw during concurrent mutation.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> does not throw while a concurrent dequeuer is mutating the buffer.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDequeueInProgress_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(6);
        for (var i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource();

        var dequeuer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                buffer.TryDequeue(out _);
                Thread.SpinWait(50);
            }
        });

        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                try { _ = buffer.Contains(new TestItem(-1)); } // always missing by ref
                catch (Exception ex) { exceptions.Add(ex); }
                Thread.SpinWait(10);
            }
            cts.Cancel();
        });

        Task.WaitAll(dequeuer, reader);

        Assert.IsEmpty(exceptions, "Contains should not throw while dequeues are in progress.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> remains <see langword="true" /> for a value while at least one matching instance is still in the buffer.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDuplicatesPresent_ShouldReturnTrueWhileAnyInstanceExists()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        var a = new TestItem(1);
        var b = new TestItem(1); // distinct reference, same Value

        buffer.Enqueue(a);
        buffer.Enqueue(b);
        Assert.IsTrue(buffer.Contains(a));
        Assert.IsTrue(buffer.Contains(b));

        // Remove one instance
        buffer.TryDequeue(out _);

        // Still true because the other instance remains
        Assert.IsTrue(buffer.Contains(b));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> consistently returns <see langword="false" /> for items that were never enqueued.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemAbsent_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var missing = new TestItem(999);
        var result = Enumerable.Range(0, 1000).AsParallel().All(_ => !buffer.Contains(missing));

        Assert.IsTrue(result, "Contains returned true for missing item.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> eventually returns <see langword="true" /> for an item introduced by a concurrent enqueuer.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemEnqueuedConcurrently_ShouldEventuallyReturnTrue()
    {
        // Capacity exceeds the total number of items written so the target is never
        // evicted before the reader can observe it, regardless of thread scheduling.
        var buffer = new ConcurrentCircularBuffer<TestItem>(100);
        var target = new TestItem(999);
        var found = false;
        using var targetEnqueued = new ManualResetEventSlim(false);

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                buffer.TryEnqueue(i == 25 ? target : new TestItem(i));

                // Signal the reader the instant the target lands in the buffer so it does
                // not waste its polling budget before the target is observable.
                if (i == 25)
                    targetEnqueued.Set();

                Thread.SpinWait(50);
            }
        });

        var reader = Task.Run(() =>
        {
            // Do not begin polling until the target has been confirmed enqueued; without
            // this gate the reader can exhaust all iterations before the writer reaches i == 25.
            targetEnqueued.Wait();

            for (var i = 0; i < 200; i++)
            {
                if (buffer.Contains(target))
                {
                    found = true;
                    break;
                }
                Thread.SpinWait(10);
            }
        });

        Task.WaitAll(writer, reader);
        Assert.IsTrue(found, "Target item was never detected by Contains.");
    }


    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> returns <see langword="false" /> after an item has been evicted by overwriting.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemEvictedByOverwrite_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        var target = new TestItem(100);

        buffer.Enqueue(target);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        // Overwrite to evict 'target'
        buffer.Enqueue(new TestItem(3)); // evicts target

        Assert.IsFalse(buffer.Contains(target),
            "Contains should return false once the target has been evicted.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> handles <see langword="null" /> and non-null items correctly when both are present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenMultipleNullsAndValues_ShouldHandleNullEqualityCorrectly()
    {
        var buffer = new ConcurrentCircularBuffer<string>(6);
        buffer.Enqueue(null);
        buffer.Enqueue("x");
        buffer.Enqueue(null);
        buffer.Enqueue("y");

        Assert.IsTrue(buffer.Contains(null));
        Assert.IsTrue(buffer.Contains("x"));
        Assert.IsFalse(buffer.Contains("z"));
    }

    /// <summary>
    /// Verifies that Contains uses reference equality for types that do not override Equals, meaning two distinct
    /// instances with equivalent state are not considered equal.
    /// </summary>
    [TestMethod]
    public void Contains_WhenReferenceTypeDoesNotOverrideEquals_ShouldUseReferenceEquality()
    {
        // Use a local type that does not override Equals — equality falls back to reference identity
        var buffer = new ConcurrentCircularBuffer<ReferenceItem>(3);
        var enqueued = new ReferenceItem(1);
        buffer.Enqueue(enqueued);

        var differentInstanceSameValue = new ReferenceItem(1);

        Assert.IsFalse(buffer.Contains(differentInstanceSameValue),
            "Contains should return false for a different instance with the same state when Equals is not overridden, " +
            "because EqualityComparer<T>.Default falls back to reference identity.");

        Assert.IsTrue(buffer.Contains(enqueued),
            "Contains should return true for the exact same instance that was enqueued.");
    }

    /// <summary>
    /// Verifies that Contains returns true for a different instance that has the same value as an enqueued element,
    /// because TestItem overrides Equals to compare by value and EqualityComparer{T}.Default delegates to Equals.
    /// </summary>
    [TestMethod]
    public void Contains_WhenReferenceTypeOverridesEquals_ShouldUseValueEquality()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var differentInstanceSameValue = new TestItem(1);

        Assert.IsTrue(buffer.Contains(differentInstanceSameValue),
            "Contains should return true for a different instance with the same value because TestItem.Equals " +
            "compares by value and EqualityComparer<T>.Default delegates to Equals.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> uses the default equality comparer for value types.
    /// </summary>
    [TestMethod]
    public void Contains_WhenUsingValueTypes_ShouldUseDefaultEquality()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        Assert.IsTrue(buffer.Contains(new TestItem(20)));
        Assert.IsFalse(buffer.Contains(new TestItem(99)));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> correctly finds items that straddle a wraparound boundary and excludes items that were dequeued.
    /// </summary>
    [TestMethod]
    public void Contains_WhenWrapAroundOccurs_ShouldFindItemsAcrossBoundary()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        var a = new TestItem(1);
        var b = new TestItem(2);
        var c = new TestItem(3);

        buffer.Enqueue(a);
        buffer.Enqueue(b);
        buffer.Enqueue(c);

        // Cause wrap by dequeuing one and enqueueing another
        buffer.TryDequeue(out _);
        var d = new TestItem(4);
        buffer.Enqueue(d);

        Assert.IsTrue(buffer.Contains(b));
        Assert.IsTrue(buffer.Contains(c));
        Assert.IsTrue(buffer.Contains(d));
        Assert.IsFalse(buffer.Contains(a)); // was dequeued
    }

}
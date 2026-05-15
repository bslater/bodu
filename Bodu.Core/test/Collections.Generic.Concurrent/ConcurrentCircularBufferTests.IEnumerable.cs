// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that enumeration includes <see langword="null" /> entries among the buffer's items.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBufferContainsNull_ShouldIncludeNull()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(3));

        TestItem?[] items = buffer.ToArray();
        Assert.AreEqual(3, items.Length);
        Assert.IsTrue(items.Any(i => i is null));
    }

    /// <summary>
    /// Verifies that enumerating an empty buffer yields no items.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBufferEmpty_ShouldYieldNoItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        TestItem[] snapshot = buffer.ToArray();
        Assert.AreEqual(0, snapshot.Length);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> under a concurrent dequeuer never throws and returns a snapshot whose length is within capacity.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenConcurrentDequeueInProgress_ShouldReturnBoundedSnapshotAndNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var errors = new ConcurrentBag<Exception>();
        var start = new ManualResetEventSlim(false);

        var dequeuer = Task.Run(() =>
        {
            try
            {
                start.Wait();
                for (var i = 0; i < 10; i++)
                    buffer.TryDequeue(out TestItem? _);
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        var enumerator = Task.Run(() =>
        {
            try
            {
                start.Wait();
                TestItem[] snapshot = buffer.ToArray();
                Assert.IsTrue(snapshot.Length <= buffer.Capacity);
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        start.Set();
        Task.WaitAll(dequeuer, enumerator);

        Assert.AreEqual(0, errors.Count, $"Unexpected exceptions: {string.Join(", ", errors.Select(e => e.GetType().Name))}");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> under a concurrent enqueuer never throws and every snapshot stays within capacity.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenConcurrentEnqueueInProgress_ShouldNotThrowAndReturnBoundedSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(50);
        var snapshotLengths = new ConcurrentBag<int>();
        var errors = new ConcurrentBag<Exception>();
        var start = new ManualResetEventSlim(false);

        var writer = Task.Run(() =>
        {
            try
            {
                start.Wait();
                for (var i = 0; i < 100; i++)
                    buffer.Enqueue(new TestItem(i));
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        var reader = Task.Run(() =>
        {
            try
            {
                start.Wait();
                for (var i = 0; i < 20; i++)
                {
                    TestItem[] snapshot = buffer.ToArray();
                    snapshotLengths.Add(snapshot.Length);
                    Thread.SpinWait(10);
                }
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        start.Set();
        Task.WaitAll(writer, reader);

        Assert.AreEqual(0, errors.Count, $"Unexpected exceptions: {string.Join(", ", errors.Select(e => e.GetType().Name))}");
        Assert.IsTrue(snapshotLengths.All(len => len <= buffer.Capacity));
    }

    /// <summary>
    /// Verifies that the enumerator output from <see cref="Enumerable.ToArray{TSource}"/> matches the output from
    /// <see cref="ConcurrentCircularBuffer{T}.CopyTo"/> when the buffer is in a contiguous (non-wrapped) state.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenContiguous_ShouldMatchCopyToOutput()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        TestItem[] fromToArray = buffer.ToArray();

        var fromCopyTo = new TestItem[buffer.Count];
        buffer.CopyTo(fromCopyTo, 0);

        CollectionAssert.AreEqual(fromToArray, fromCopyTo);
    }

    /// <summary>
    /// Verifies that LINQ enumeration over the buffer yields items in FIFO order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumerated_ShouldYieldAllItemsInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var values = buffer.Select(x => x?.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
    }

    /// <summary>
    /// Verifies that the enumerator output from <see cref="Enumerable.ToArray{TSource}"/> matches the output from
    /// <see cref="ConcurrentCircularBuffer{T}.CopyTo"/> when the internal buffer has wrapped around.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenWrapped_ShouldMatchCopyToOutput()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue();       // removes 1
        buffer.Enqueue(new TestItem(4));      // wraps: logical order is [2, 3, 4]

        TestItem[] fromToArray = buffer.ToArray();

        var fromCopyTo = new TestItem[buffer.Count];
        buffer.CopyTo(fromCopyTo, 0);

        CollectionAssert.AreEqual(fromToArray, fromCopyTo);
    }

}
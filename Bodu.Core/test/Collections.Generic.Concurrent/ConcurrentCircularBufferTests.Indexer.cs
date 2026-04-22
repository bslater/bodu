// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    [TestMethod]
    public void Indexer_WhenAccessingNullItem_ShouldReturnNull()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(2);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));

        Assert.IsNull(buffer[0]);
        Assert.AreEqual(1, buffer[1]!.Value);
    }

    [TestMethod]
    public void Indexer_WhenEnumeratingValidRange_ShouldReturnExpectedValuesInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        Assert.AreEqual(10, buffer[0].Value);
        Assert.AreEqual(20, buffer[1].Value);
        Assert.AreEqual(30, buffer[2].Value);
    }

    [TestMethod]
    public void Indexer_WhenNegativeIndex_ShouldThrowArgumentOutOfRange()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            var _ = buffer[-1];
        });
    }

    [TestMethod]
    public void Indexer_WhenNonNegativeIndexBeyondSnapshot_ShouldThrowArgumentOutOfRange()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1)); // snapshot length at least 1

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            var _ = buffer[1]; // out of range for the point-in-time snapshot
        });
    }

    [TestMethod]
    public void Indexer_WhenReadConcurrentlyWithoutMutation_ShouldBeStableForInitialSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var errors = new ConcurrentBag<Exception>();

        Parallel.For(0, 10, i =>
        {
            try
            {
                var item = buffer[i];
                Assert.IsNotNull(item);
                Assert.AreEqual(i, item.Value);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Indexer_WhenReadDuringConcurrentEnqueue_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var errors = new ConcurrentBag<Exception>();
        var start = new ManualResetEventSlim(false);

        var reader = Task.Run(() =>
        {
            try
            {
                start.Wait();
                for (int i = 0; i < 100; i++)
                {
                    _ = buffer[0]; // may be default if snapshot shrinks
                    Thread.SpinWait(5);
                }
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        var writer = Task.Run(() =>
        {
            start.Wait();
            for (int i = 10; i < 110; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                Thread.SpinWait(5);
            }
        });

        start.Set();
        Task.WaitAll(reader, writer);
        Assert.AreEqual(0, errors.Count, "Indexer threw during concurrent enqueue.");
    }
}
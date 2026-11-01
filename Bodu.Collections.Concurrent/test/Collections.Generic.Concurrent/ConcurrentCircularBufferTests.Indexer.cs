// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> when the requested slot contains a <see langword="null" /> entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAccessingNullItem_ShouldReturnNull()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(2);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));

        Assert.IsNull(buffer[0]);
        Assert.AreEqual(1, buffer[1]!.Value);
    }

    /// <summary>
    /// Documents the indexer contract: two consecutive index reads are not jointly atomic. A producer or
    /// consumer running between the two calls may shift the live window, so callers needing joint atomicity
    /// across multiple positions must take a single <c>ToArray</c> snapshot and index the resulting array.
    /// </summary>
    /// <remarks>
    /// In the absence of concurrent mutation each call is well-defined and consecutive reads observe the
    /// expected adjacent values. This test records the contract rather than asserting non-atomicity directly,
    /// which would require a deterministic interleaving harness.
    /// </remarks>
    [TestMethod]
    public void Indexer_WhenCalledTwiceConsecutivelyOnQuiescentBuffer_ShouldReturnAdjacentValues()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(100));
        buffer.Enqueue(new TestItem(200));
        buffer.Enqueue(new TestItem(300));

        TestItem first = buffer[0];
        TestItem second = buffer[1];

        Assert.AreEqual(100, first.Value);
        Assert.AreEqual(200, second.Value);
    }

    /// <summary>
    /// Verifies that the indexer returns items in FIFO order across the full valid index range.
    /// </summary>
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

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentOutOfRangeException" /> when given a negative index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenNegativeIndex_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            TestItem _ = buffer[-1];
        });
    }

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentOutOfRangeException" /> when given an index beyond the current snapshot's length.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenNonNegativeIndexBeyondSnapshot_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1)); // snapshot length at least 1

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            TestItem _ = buffer[1]; // out of range for the point-in-time snapshot
        });
    }

    /// <summary>
    /// Verifies that parallel indexer reads on an unchanging buffer return the correct values for every valid index.
    /// </summary>
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
                TestItem item = buffer[i];
                Assert.IsNotNull(item);
                Assert.AreEqual(i, item.Value);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.IsEmpty(errors);
    }

    /// <summary>
    /// Verifies that indexer reads do not throw while a concurrent enqueuer is mutating the buffer.
    /// </summary>
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
        Assert.IsEmpty(errors, "Indexer threw during concurrent enqueue.");
    }

    /// <summary>
    /// Verifies that when the head is observed past the tail - the two positions read at different moments while
    /// consumers drain the buffer - the indexer clamps the count to zero and rejects every index as out of range.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenHeadObservedPastTail_ShouldClampCountToZeroAndThrowArgumentOutOfRangeException()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.SetPositions(head: 5, tail: 3);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = buffer[0];
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that when the tail is observed more than <see cref="ConcurrentCircularBuffer{T}.Capacity" /> ahead of
    /// the head - a stale head read against producers that have wrapped - the indexer clamps the count to the
    /// capacity, so the last slot of the ring is reachable and the index after it is out of range.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenTailObservedMoreThanCapacityAheadOfHead_ShouldClampCountToCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        for (int i = 0; i < 4; i++)
            buffer.Enqueue(new TestItem(i));
        buffer.SetPositions(head: 0, tail: 4 + 3);

        Assert.AreEqual(3, buffer[3].Value, "The last slot of the clamped window must read its published element.");
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = buffer[4];
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that when the slot at the requested position never carries its publication mark - a slot a producer
    /// has claimed but not yet published, on every attempt - the indexer exhausts its retry budget and throws
    /// <see cref="InvalidOperationException" /> with the snapshot-unstable message rather than returning a torn value,
    /// while the stable slot after it still reads normally.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSlotNeverCarriesItsPublicationMark_ShouldThrowInvalidOperationException()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.SetSlotSequence(slotIndex: 0, sequence: 0);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = buffer[0];
        });

        Assert.AreEqual(ConcurrentCollectionsResourceStrings.Op_Invalid_ConcurrentSnapshotUnstable, ex.Message);
        Assert.AreEqual(2, buffer[1].Value);
    }

}
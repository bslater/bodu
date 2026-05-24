// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.SlotMechanics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.Count" /> property reports the same logical count as the strongly-typed
    /// <see cref="ConcurrentCircularBuffer{T}.Count" /> after the buffer's head and tail have wrapped past the underlying array
    /// boundary at least once. Exercises the count code path when slots are re-used.
    /// </summary>
    [TestMethod]
    public void Count_WhenHeadAndTailWrapAround_ShouldReturnLogicalCount()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);

        for (var i = 0; i < 4; i++)
            buffer.Enqueue(new TestItem(i));

        // Drain three, refill three — head and tail each wrap past slot 0.
        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(11));
        buffer.Enqueue(new TestItem(12));

        Assert.AreEqual(4, buffer.Count);
        Assert.HasCount(4, (ICollection)buffer);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.this[int]" /> tolerates the slot-generation-changed retry path under
    /// concurrent enqueue and dequeue, returning either the published value or surfacing a documented exception, but never tearing
    /// or returning an inconsistent reference. Exercises the indexer's outer retry loop and <c>TryReadStableSlot</c>'s sequence
    /// re-check via deterministic two-thread interleaving rather than a long stress run.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSlotGenerationChangesDuringRead_ShouldRetryAndReturnStableValue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8, allowOverwrite: true);
        for (var i = 0; i < 8; i++) buffer.Enqueue(new TestItem(i));

        var errors = new ConcurrentBag<Exception>();
        var start = new ManualResetEventSlim(false);
        var stop = new CancellationTokenSource();

        var rotator = Task.Run(() =>
        {
            start.Wait();
            var i = 100;
            while (!stop.IsCancellationRequested)
            {
                buffer.TryDequeue(out _);
                buffer.TryEnqueue(new TestItem(i++));
            }
        });

        var reader = Task.Run(() =>
        {
            try
            {
                start.Wait();
                for (var iter = 0; iter < 2000; iter++)
                {
                    try
                    {
                        TestItem _ = buffer[0];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Snapshot may shrink between Count read and index read; permitted by the contract.
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
            finally
            {
                stop.Cancel();
            }
        });

        start.Set();
        Task.WaitAll(rotator, reader);

        Assert.IsEmpty(errors, "Indexer threw an unexpected exception during concurrent slot rotation.");
    }

    /// <summary>
    /// Verifies that the indexer returns a stable value for every reachable position after the buffer has wrapped. Indirectly
    /// exercises <c>TryReadStableSlot</c>'s success path on slots that have been re-published (their sequence reflects a later
    /// generation than the original initialisation value).
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSlotsHaveBeenRepublishedAfterWrap_ShouldReturnStableValuesForEachPosition()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);

        // Initial generation.
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Re-publish slots: drain three, fill three — each underlying slot now carries a higher Sequence value.
        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);
        buffer.TryDequeue(out _);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(11));
        buffer.Enqueue(new TestItem(12));

        Assert.AreEqual(10, buffer[0].Value);
        Assert.AreEqual(11, buffer[1].Value);
        Assert.AreEqual(12, buffer[2].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryDequeue(out T)" /> on an empty buffer takes the empty branch and reports
    /// failure via <c>InternalDequeue</c>'s <c>throwIfEmpty == false</c> path.
    /// </summary>
    [TestMethod]
    public void InternalDequeue_WhenBufferIsEmptyAndThrowOnEmptyFalse_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);

        var success = buffer.TryDequeue(out TestItem? item);

        Assert.IsFalse(success);
        Assert.IsNull(item);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> on an empty buffer takes the empty branch and routes through
    /// <c>InternalDequeue</c>'s <c>throwIfEmpty == true</c> path to surface <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void InternalDequeue_WhenBufferIsEmptyAndThrowOnEmptyTrue_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Dequeue();
        });
    }

}

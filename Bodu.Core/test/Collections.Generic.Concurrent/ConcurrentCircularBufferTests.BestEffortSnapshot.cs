// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.BestEffortSnapshot.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the private <c>BestEffortSnapshot</c> fallback returns an empty array when invoked on an empty buffer.
    /// </summary>
    [TestMethod]
    public void BestEffortSnapshot_WhenBufferIsEmpty_ShouldReturnEmptyArray()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);

        TestItem[] snapshot = InvokeBestEffortSnapshot(buffer);

        Assert.IsEmpty(snapshot);
    }

    /// <summary>
    /// Verifies that the private <c>BestEffortSnapshot</c> fallback returns the live elements in FIFO order when no concurrent
    /// mutation is interfering with the slot reads, exercising the happy-path branch of the fallback.
    /// </summary>
    [TestMethod]
    public void BestEffortSnapshot_WhenBufferIsQuiescent_ShouldReturnExpectedFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        TestItem[] snapshot = InvokeBestEffortSnapshot(buffer);

        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            snapshot.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that the private <c>BestEffortSnapshot</c> fallback writes <see langword="null" /> for any slot whose sequence number
    /// disagrees with the expected publication mark — the failure mode the fallback exists to handle — rather than reading a
    /// torn or stale-generation value.
    /// </summary>
    [TestMethod]
    public void BestEffortSnapshot_WhenSlotCannotBeStabilised_ShouldWriteDefaultForThatSlot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Corrupt the sequence of the middle slot so TryReadStableSlot rejects it as not-yet-published.
        // The seqlock pre-check (seqPre != expectedSequence) makes BestEffortSnapshot fall back to
        // default(T) for that slot, leaving the surrounding slots intact.
        CorruptSlotSequence(buffer, slotIndex: 1, newSequence: int.MinValue);

        TestItem[] snapshot = InvokeBestEffortSnapshot(buffer);

        Assert.HasCount(3, snapshot);
        Assert.IsNotNull(snapshot[0]);
        Assert.AreEqual(1, snapshot[0].Value);
        Assert.IsNull(snapshot[1]);
        Assert.IsNotNull(snapshot[2]);
        Assert.AreEqual(3, snapshot[2].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Contains" /> returns <see langword="false" /> via the fallback snapshot
    /// scan when every slot is forced into an unstable state — exercising the post-outer-retry-budget fallback path
    /// (<c>foreach (T? x in ToArray()) ...</c>) at the end of <c>Contains</c>.
    /// </summary>
    [TestMethod]
    public void Contains_WhenAllSlotsAreUnstable_ShouldFallBackToSnapshotScanAndReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        var target = new TestItem(42);
        buffer.Enqueue(target);
        buffer.Enqueue(new TestItem(99));

        // Corrupt every slot so the in-place seqlock scan inside Contains exhausts its outer
        // retry budget; the fallback path then reads via ToArray (which itself falls back to
        // BestEffortSnapshot, writing default for each unstable slot). Default values cannot
        // match a real element so the final result is false.
        for (var i = 0; i < 3; i++)
            CorruptSlotSequence(buffer, slotIndex: i, newSequence: int.MinValue);

        Assert.IsFalse(buffer.Contains(target));
    }

    /// <summary>
    /// Verifies that the public <see cref="ConcurrentCircularBuffer{T}.ToArray" /> method falls through to <c>BestEffortSnapshot</c>
    /// and returns a same-length array of default values when every slot's sequence has been forced into an unstable state, exercising
    /// the <c>return BestEffortSnapshot();</c> branch after the outer retry budget is exhausted.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenAllSlotsAreUnstable_ShouldFallBackToBestEffortSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Force every slot's published sequence to a non-matching value. With no slot satisfying
        // the expected publication mark, the seqlock pre-check fails on every retry, exhausting the
        // outer retry budget and steering ToArray into the BestEffortSnapshot fallback.
        for (var i = 0; i < 3; i++)
            CorruptSlotSequence(buffer, slotIndex: i, newSequence: int.MinValue);

        TestItem[] snapshot = buffer.ToArray();

        Assert.HasCount(3, snapshot);
        foreach (TestItem item in snapshot)
            Assert.IsNull(item);
    }

    /// <summary>
    /// Overwrites the <c>Sequence</c> field of the slot at <paramref name="slotIndex"/> in <paramref name="buffer"/>'s internal
    /// array to <paramref name="newSequence"/> via reflection, simulating a torn or unpublished slot that the seqlock pre-check
    /// will reject.
    /// </summary>
    /// <typeparam name="T">The buffer element type.</typeparam>
    /// <param name="buffer">The buffer whose slot to corrupt.</param>
    /// <param name="slotIndex">The zero-based physical slot index to modify.</param>
    /// <param name="newSequence">The sequence value to write into the slot.</param>
    private static void CorruptSlotSequence<T>(ConcurrentCircularBuffer<T> buffer, int slotIndex, int newSequence)
        where T : class?
    {
        FieldInfo bufferField = typeof(ConcurrentCircularBuffer<T>).GetField(
            "_buffer",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_buffer field not found.");

        var slotArray = (Array)bufferField.GetValue(buffer)!;
        var slot = slotArray.GetValue(slotIndex)!;
        FieldInfo sequenceField = slot.GetType().GetField("Sequence", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Slot.Sequence field not found.");

        sequenceField.SetValue(slot, newSequence);
        slotArray.SetValue(slot, slotIndex);
    }

    /// <summary>
    /// Invokes the private <c>BestEffortSnapshot</c> method on <paramref name="buffer"/> via reflection.
    /// </summary>
    /// <typeparam name="T">The buffer element type.</typeparam>
    /// <param name="buffer">The buffer to snapshot.</param>
    /// <returns>The best-effort snapshot produced by the private fallback.</returns>
    private static T[] InvokeBestEffortSnapshot<T>(ConcurrentCircularBuffer<T> buffer)
        where T : class?
    {
        MethodInfo method = typeof(ConcurrentCircularBuffer<T>).GetMethod(
            "BestEffortSnapshot",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BestEffortSnapshot method not found.");

        return (T[])method.Invoke(buffer, [])!;
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that concurrent reads of <see cref="ConcurrentCircularBuffer{T}.Capacity" /> all return the same configured value.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenAccessedConcurrently_ShouldReturnConsistentValue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(32);
        var results = new int[1000];

        Parallel.For(0, 1000, i => results[i] = buffer.Capacity);

        Assert.IsTrue(results.All(r => r == 32), "All reads of Capacity should return 32.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Capacity" /> stays constant even while <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> is toggled concurrently.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenAllowOverwriteToggles_ShouldRemainConstant()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(9, allowOverwrite: false);

        Parallel.For(0, 2000, i =>
        {
            buffer.AllowOverwrite = (i & 1) == 0;
            _ = buffer.Capacity;
        });

        Assert.AreEqual(9, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Capacity" /> returns the value passed at construction.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenBufferConstructed_ShouldReturnDefinedValue()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        Assert.AreEqual(10, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that a collection-initialised buffer reports the explicitly supplied capacity, even when the source is trimmed to fit.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenConstructedFromCollection_ShouldUseProvidedCapacity()
    {
        TestItem[] source = Enumerable.Range(0, 10).Select(i => new TestItem(i)).ToArray();
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 6, allowOverwrite: true);

        Assert.AreEqual(6, buffer.Capacity);

        // Data may be truncated to newest 6, but capacity itself must be 6
        CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9 }, buffer.ToArray().Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Capacity" /> is immutable while enqueue and dequeue run concurrently.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenEnqueueAndDequeueConcurrently_ShouldRemainStable()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);

        Parallel.Invoke(
            () =>
            {
                for (var i = 0; i < 100; i++)
                {
                    buffer.TryEnqueue(new TestItem(i));
                    Thread.SpinWait(100);
                }
            },
            () =>
            {
                for (var i = 0; i < 100; i++)
                {
                    buffer.TryDequeue(out _);
                    Thread.SpinWait(100);
                }
            });

        Assert.AreEqual(8, buffer.Capacity, "Capacity must remain fixed despite concurrent mutation.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Capacity" /> is preserved across a fill/<see cref="ConcurrentCircularBuffer{T}.Clear" /> cycle.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenFilledAndCleared_ShouldRemainUnchanged()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);

        for (var i = 0; i < 5; i++)
            buffer.Enqueue(new TestItem(i));

        buffer.Clear();

        Assert.AreEqual(5, buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Capacity" /> is unaffected by overwriting evictions.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenOverwriteEvictsItems_ShouldRemainUnchanged()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Enqueue(new TestItem(4)); // evicts 1
        buffer.Enqueue(new TestItem(5)); // evicts 2

        Assert.AreEqual(3, buffer.Capacity);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, buffer.ToArray().Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that snapshots returned by <see cref="ConcurrentCircularBuffer{T}.ToArray" /> never exceed <see cref="ConcurrentCircularBuffer{T}.Capacity" /> even when the buffer has been overfilled.
    /// </summary>
    [TestMethod]
    public void Capacity_Get_WhenSnapshotOperationsOccur_ShouldNeverReturnValueGreaterThanDefinedCapacity()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(7, allowOverwrite: true);

        // Overfill intentionally
        for (var i = 0; i < 100; i++)
            buffer.Enqueue(new TestItem(i));

        TestItem[] snap = buffer.ToArray();
        Assert.IsLessThanOrEqualTo(buffer.Capacity, snap.Length, "Snapshot must never exceed Capacity.");
        Assert.AreEqual(7, buffer.Capacity);
    }

}
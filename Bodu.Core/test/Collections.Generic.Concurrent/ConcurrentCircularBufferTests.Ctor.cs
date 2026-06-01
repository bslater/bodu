// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the default constructor, when invoked concurrently, produces buffers that each carry the default capacity and overwrite flag.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_WhenUsedInParallel_ShouldApplyDefaultsAndRemainStable()
    {
        var results = new ConcurrentBag<ConcurrentCircularBuffer<TestItem>>();

        Parallel.For(0, 50, _ =>
        {
            var buffer = new ConcurrentCircularBuffer<TestItem>();
            buffer.Enqueue(new TestItem(1));
            results.Add(buffer);
        });

        Assert.HasCount(50, results);
        Assert.IsTrue(results.All(b => b.Count == 1));
        Assert.IsTrue(results.All(b => b.AllowOverwrite));
        Assert.IsTrue(results.All(b => b.Capacity >= 2)); // DefaultCapacity is internal; ensure minimum is met
    }

    /// <summary>
    /// Verifies that <c>allowOverwrite: false</c> is honoured by every instance when the constructor is invoked from many threads concurrently.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllowOverwriteFalse_ShouldRespectFlagAcrossParallelConstruction()
    {
        var results = new ConcurrentBag<ConcurrentCircularBuffer<TestItem>>();

        Parallel.For(0, 20, _ =>
        {
            var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: false);
            buffer.Enqueue(new TestItem(1));
            results.Add(buffer);
        });

        Assert.IsTrue(results.All(b => !b.AllowOverwrite));
        Assert.IsTrue(results.All(b => b.Count == 1));
    }

    /// <summary>
    /// Verifies that constructing from a source collection that fits within capacity preserves every item in enumeration order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllowOverwriteTrueAndSourceFits_ShouldPreserveAllItems()
    {
        IEnumerable<TestItem> source = Enumerable.Range(10, 3).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 10, 11, 12 }, values);
        Assert.AreEqual(3, buffer.Count);
    }

    // Minimum capacity is now 2. DataRow values cover all values below the minimum:
    // negative, zero, and the boundary value of 1 that was previously accepted but is now rejected.

    /// <summary>
    /// Verifies that a capacity below the minimum (2) throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void Ctor_WhenCapacityIsLessThanTwo_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(capacity);
        });
    }

    // The collection constructor delegates capacity validation to the two-parameter base constructor,
    // so any capacity < 2 must throw regardless of the source contents or allowOverwrite flag.

    /// <summary>
    /// Verifies that the collection constructor enforces the capacity minimum regardless of the <c>allowOverwrite</c> flag, throwing <see cref="ArgumentOutOfRangeException" /> for any capacity below 2.
    /// </summary>
    [TestMethod]
    [DataRow(-5, true)]
    [DataRow(-1, true)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(-5, false)]
    [DataRow(-1, false)]
    [DataRow(0, false)]
    [DataRow(1, false)]
    public void Ctor_WhenCapacityIsLessThanTwo_WithSourceEnumerable_ShouldThrowExactly(
        int capacity, bool allowOverwrite)
    {
        TestItem[] empty = [];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(empty, capacity, allowOverwrite);
        });
    }

    // Capacity = 2 is the minimum accepted value. Verify it constructs and behaves correctly.

    /// <summary>
    /// Verifies that the minimum-valid capacity of <c>2</c> constructs successfully and evicts in FIFO order when overfilled.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsTwo_ShouldConstructSuccessfullyAndAcceptItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2)); // full
        buffer.Enqueue(new TestItem(3)); // evicts 1 → [2, 3]
        buffer.Enqueue(new TestItem(4)); // evicts 2 → [3, 4]

        var arr = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 3, 4 }, arr);
        Assert.AreEqual(2, buffer.Capacity);
        Assert.AreEqual(2, buffer.Count);
    }

    /// <summary>
    /// Verifies that a capacity-taking constructor, invoked from many threads, initialises each instance consistently.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityProvided_AndConstructedInParallel_ShouldInitializeConsistently()
    {
        var results = new ConcurrentBag<ConcurrentCircularBuffer<TestItem>>();

        Parallel.For(0, 100, _ =>
        {
            var buffer = new ConcurrentCircularBuffer<TestItem>(5);
            buffer.Enqueue(new TestItem(1));
            results.Add(buffer);
        });

        Assert.HasCount(100, results);
        Assert.IsTrue(results.All(b => b.Capacity == 5));
        Assert.IsTrue(results.All(b => b.Count == 1));
    }

    /// <summary>
    /// Verifies that when the source contains more items than the buffer's capacity, construction retains only the most recent items in FIFO order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerable_WithCapacitySmallerThanSource_ShouldTrimToMostRecent()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 5).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 3); // allowOverwrite defaults to true

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, values);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> entries in the source collection are preserved in the constructed buffer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerableContainingNulls_ShouldRetainNulls()
    {
        var source = new TestItem?[] { new(1), null, new(3) };
        var buffer = new ConcurrentCircularBuffer<TestItem?>(source, 4);

        TestItem?[] arr = buffer.ToArray();
        Assert.HasCount(3, arr);
        Assert.IsNotNull(arr[0]);
        Assert.IsNull(arr[1]);
        Assert.IsNotNull(arr[2]);
    }

    /// <summary>
    /// Verifies that the three-argument constructor applies the specified capacity, allow-overwrite flag, and source contents together.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerableWithCapacityAndFlag_ShouldRespectAllValues()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 3).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 3, false);

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(3, buffer.Count);
        Assert.IsFalse(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that constructing from a non-array enumerable (a lazy LINQ sequence) preserves the source's enumeration order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructedFromNonArrayEnumerable_ShouldPreserveOrder()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 3).Select(x => new TestItem(x));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 5);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="ConcurrentCircularBuffer{T}"/> with an empty collection and a negative
    /// capacity throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptyCollectionAndInvalidCapacity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>([], -1);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="ConcurrentCircularBuffer{T}"/> with an empty collection and a valid capacity
    /// creates an empty buffer with the specified capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptyCollectionAndValidCapacity_ShouldCreateEmptyBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>([], 5);

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> source to the capacity-taking constructor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEnumerableIsNull_WithCapacity_ShouldThrowExactly()
    {
        IEnumerable<TestItem> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3);
        });
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> source to the three-argument constructor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEnumerableIsNull_WithCapacityAndAllowOverwrite_ShouldThrowExactly()
    {
        IEnumerable<TestItem> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3, allowOverwrite: true);
        });
    }

    /// <summary>
    /// Verifies that a source exceeding capacity with overwriting disabled throws <see cref="InvalidOperationException" /> during construction.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteFalse_ShouldThrowExactly()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 5).Select(i => new TestItem(i));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3, allowOverwrite: false);
        });
    }

    /// <summary>
    /// Verifies that a source exceeding capacity with overwriting enabled keeps only the most recent items in FIFO order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteTrue_ShouldKeepMostRecentInFifoOrder()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 8).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8 }, values);
        Assert.AreEqual(5, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that the ctor's implicit trimming of an oversized source does not raise <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteTrue_ShouldNotRaiseEvictionEvents()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 10).Select(i => new TestItem(i));
        var evicted = 0;

        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);
        buffer.ItemEvicted += _ => evicted++;

        // Verify contents are the last 5 (6..10) and no ctor-time events fired.
        CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10 }, buffer.ToArray().Select(x => x.Value).ToArray());
        Assert.AreEqual(0, evicted);
    }

    /// <summary>
    /// Verifies that a buffer constructed exactly at capacity from a source collection accepts subsequent
    /// producer-side eviction (when overwrite is enabled), confirming the per-slot publication state recorded
    /// by the construction-time fill is observable by the lock-free producer protocol.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceFillsCapacity_ShouldRemainConsistentWithSubsequentEnqueue()
    {
        TestItem[] source = Enumerable.Range(1, 4).Select(i => new TestItem(i)).ToArray();
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 4, allowOverwrite: true);

        Assert.AreEqual(4, buffer.Count);
        Assert.AreEqual(4, buffer.Capacity);

        buffer.Enqueue(new TestItem(5));

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, values);
    }

    /// <summary>
    /// Verifies that a buffer constructed from a source larger than its capacity retains only the trailing
    /// window and that subsequent <c>Dequeue</c> operations return elements in the correct FIFO order from that
    /// window.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceLargerThanCapacityAndAllowOverwriteTrue_ShouldKeepTrailingWindowAndDequeueInOrder()
    {
        TestItem[] source = Enumerable.Range(1, 7).Select(i => new TestItem(i)).ToArray();
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 3, allowOverwrite: true);

        Assert.AreEqual(3, buffer.Count);
        Assert.AreEqual(5, buffer.Dequeue().Value);
        Assert.AreEqual(6, buffer.Dequeue().Value);
        Assert.AreEqual(7, buffer.Dequeue().Value);
        Assert.AreEqual(0, buffer.Count);
    }

}
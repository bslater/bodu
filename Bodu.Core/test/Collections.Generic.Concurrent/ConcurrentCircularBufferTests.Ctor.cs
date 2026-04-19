using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
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

        Assert.AreEqual(50, results.Count);
        Assert.IsTrue(results.All(b => b.Count == 1));
        Assert.IsTrue(results.All(b => b.AllowOverwrite));
        Assert.IsTrue(results.All(b => b.Capacity >= 2)); // DefaultCapacity is internal; ensure minimum is met
    }

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

        Assert.IsTrue(results.All(b => b.AllowOverwrite == false));
        Assert.IsTrue(results.All(b => b.Count == 1));
    }

    [TestMethod]
    public void Ctor_WhenAllowOverwriteTrueAndSourceFits_ShouldPreserveAllItems()
    {
        var source = Enumerable.Range(10, 3).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 10, 11, 12 }, values);
        Assert.AreEqual(3, buffer.Count);
    }

    // Minimum capacity is now 2. DataRow values cover all values below the minimum:
    // negative, zero, and the boundary value of 1 that was previously accepted but is now rejected.

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void Ctor_WhenCapacityIsLessThanTwo_ShouldThrowArgumentOutOfRange(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(capacity);
        });
    }

    // Capacity = 2 is the minimum accepted value. Verify it constructs and behaves correctly.

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

    // The collection constructor delegates capacity validation to the two-parameter base constructor,
    // so any capacity < 2 must throw regardless of the source contents or allowOverwrite flag.

    [TestMethod]
    [DataRow(-5, true)]
    [DataRow(-1, true)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(-5, false)]
    [DataRow(-1, false)]
    [DataRow(0, false)]
    [DataRow(1, false)]
    public void Ctor_WhenCapacityIsLessThanTwo_WithSourceEnumerable_ShouldThrowArgumentOutOfRange(
        int capacity, bool allowOverwrite)
    {
        var empty = Array.Empty<TestItem>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(empty, capacity, allowOverwrite);
        });
    }

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

        Assert.AreEqual(100, results.Count);
        Assert.IsTrue(results.All(b => b.Capacity == 5));
        Assert.IsTrue(results.All(b => b.Count == 1));
    }

    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerable_WithCapacitySmallerThanSource_ShouldTrimToMostRecent()
    {
        var source = Enumerable.Range(1, 5).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 3); // allowOverwrite defaults to true

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, values);
    }

    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerableContainingNulls_ShouldRetainNulls()
    {
        var source = new TestItem?[] { new TestItem(1), null, new TestItem(3) };
        var buffer = new ConcurrentCircularBuffer<TestItem?>(source, 4);

        var arr = buffer.ToArray();
        Assert.AreEqual(3, arr.Length);
        Assert.IsNotNull(arr[0]);
        Assert.IsNull(arr[1]);
        Assert.IsNotNull(arr[2]);
    }

    [TestMethod]
    public void Ctor_WhenConstructedFromEnumerableWithCapacityAndFlag_ShouldRespectAllValues()
    {
        var source = Enumerable.Range(1, 3).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 3, false);

        Assert.AreEqual(3, buffer.Capacity);
        Assert.AreEqual(3, buffer.Count);
        Assert.IsFalse(buffer.AllowOverwrite);
    }

    [TestMethod]
    public void Ctor_WhenConstructedFromNonArrayEnumerable_ShouldPreserveOrder()
    {
        IEnumerable<TestItem> source = Enumerable.Range(1, 3).Select(x => new TestItem(x));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, 5);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
    }

    [TestMethod]
    public void Ctor_WhenEnumerableIsNull_WithCapacity_ShouldThrowArgumentNull()
    {
        IEnumerable<TestItem> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3);
        });
    }

    [TestMethod]
    public void Ctor_WhenEnumerableIsNull_WithCapacityAndAllowOverwrite_ShouldThrowArgumentNull()
    {
        IEnumerable<TestItem> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3, allowOverwrite: true);
        });
    }

    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteFalse_ShouldThrowInvalidOperation()
    {
        var source = Enumerable.Range(1, 5).Select(i => new TestItem(i));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new ConcurrentCircularBuffer<TestItem>(source, 3, allowOverwrite: false);
        });
    }

    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteTrue_ShouldKeepMostRecentInFifoOrder()
    {
        var source = Enumerable.Range(1, 8).Select(i => new TestItem(i));
        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);

        var values = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8 }, values);
        Assert.AreEqual(5, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }

    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_WithAllowOverwriteTrue_ShouldNotRaiseEvictionEvents()
    {
        var source = Enumerable.Range(1, 10).Select(i => new TestItem(i));
        var evicted = 0;

        var buffer = new ConcurrentCircularBuffer<TestItem>(source, capacity: 5, allowOverwrite: true);
        buffer.ItemEvicted += _ => evicted++;

        // Verify contents are the last 5 (6..10) and no ctor-time events fired.
        CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10 }, buffer.ToArray().Select(x => x.Value).ToArray());
        Assert.AreEqual(0, evicted);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="ConcurrentCircularBuffer{T}"/> with an empty collection and a negative
    /// capacity throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptyCollectionAndInvalidCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentCircularBuffer<int>(Array.Empty<int>(), -1);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="ConcurrentCircularBuffer{T}"/> with an empty collection and a valid capacity
    /// creates an empty buffer with the specified capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptyCollectionAndValidCapacity_ShouldCreateEmptyBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<int>(Array.Empty<int>(), 5);

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }
}
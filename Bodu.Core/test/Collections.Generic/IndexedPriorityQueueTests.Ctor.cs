// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
    /// <summary>
    /// Verifies that the parameterless constructor produces an empty queue with zero capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultUsed_ShouldCreateEmptyQueue()
    {
        var queue = new IndexedPriorityQueue<string, int>();

        Assert.AreEqual(0, queue.Count);
        Assert.AreEqual(0, queue.Capacity);
    }

    /// <summary>
    /// Verifies that supplying a positive capacity allocates the backing array up-front.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(1024)]
    public void Ctor_WhenCapacityProvided_ShouldSetCapacity(int capacity)
    {
        var queue = new IndexedPriorityQueue<string, int>(capacity);

        Assert.AreEqual(capacity, queue.Capacity);
        Assert.AreEqual(0, queue.Count);
    }

    /// <summary>
    /// Verifies that supplying a negative capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new IndexedPriorityQueue<string, int>(capacity);
        });
    }

    /// <summary>
    /// Verifies that a null priority comparer falls back to <see cref="Comparer{T}.Default" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPriorityComparerIsNull_ShouldUseDefaultComparer()
    {
        var queue = new IndexedPriorityQueue<string, int>((IComparer<int>?)null);

        Assert.AreSame(Comparer<int>.Default, queue.Comparer);
    }

    /// <summary>
    /// Verifies that a custom priority comparer is exposed via <see cref="IndexedPriorityQueue{TElement, TPriority}.Comparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPriorityComparerProvided_ShouldExposeComparer()
    {
        IComparer<int> reverse = Comparer<int>.Create((a, b) => b.CompareTo(a));
        var queue = new IndexedPriorityQueue<string, int>(0, reverse);

        Assert.AreSame(reverse, queue.Comparer);
    }

    /// <summary>
    /// Verifies that a null element comparer falls back to <see cref="EqualityComparer{T}.Default" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenElementComparerIsNull_ShouldUseDefaultElementComparer()
    {
        var queue = new IndexedPriorityQueue<string, int>(0, null, null);

        Assert.AreSame(EqualityComparer<string>.Default, queue.ElementComparer);
    }

    /// <summary>
    /// Verifies that a custom element comparer is exposed via <see cref="IndexedPriorityQueue{TElement, TPriority}.ElementComparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenElementComparerProvided_ShouldExposeElementComparer()
    {
        IEqualityComparer<string> ignoreCase = StringComparer.OrdinalIgnoreCase;
        var queue = new IndexedPriorityQueue<string, int>(0, null, ignoreCase);

        Assert.AreSame(ignoreCase, queue.ElementComparer);
    }

    /// <summary>
    /// Verifies that a null source enumerable throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new IndexedPriorityQueue<string, int>((IEnumerable<KeyValuePair<string, int>>)null!);
        });
    }

    /// <summary>
    /// Verifies that the items constructor populates the queue with all supplied pairs.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldPopulateQueue()
    {
        var items = new[]
        {
            new KeyValuePair<string, int>("c", 30),
            new KeyValuePair<string, int>("a", 10),
            new KeyValuePair<string, int>("b", 20),
        };

        var queue = new IndexedPriorityQueue<string, int>(items);

        Assert.AreEqual(3, queue.Count);
        Assert.IsTrue(queue.Contains("a"));
        Assert.IsTrue(queue.Contains("b"));
        Assert.IsTrue(queue.Contains("c"));
    }

    /// <summary>
    /// Verifies that the items constructor heapifies its input so that <see cref="IndexedPriorityQueue{TElement, TPriority}.Dequeue" /> yields ascending priorities.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldDequeueInPriorityOrder()
    {
        var items = new[]
        {
            new KeyValuePair<int, int>(1, 50),
            new KeyValuePair<int, int>(2, 10),
            new KeyValuePair<int, int>(3, 40),
            new KeyValuePair<int, int>(4, 20),
            new KeyValuePair<int, int>(5, 30),
        };

        var queue = new IndexedPriorityQueue<int, int>(items);
        var drained = DrainAll(queue);

        AssertNonDecreasing(drained);
        CollectionAssert.AreEqual(new[] { 2, 4, 5, 3, 1 }, drained.Select(p => p.Key).ToArray());
    }

    /// <summary>
    /// Verifies that a duplicate element key in the source enumerable throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsContainDuplicateKey_ShouldThrowExactly()
    {
        var items = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2),
        };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new IndexedPriorityQueue<string, int>(items);
        });
    }

    /// <summary>
    /// Verifies that a non-collection enumerable source is consumed correctly.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvidedAsEnumerable_ShouldPopulateQueue()
    {
        IEnumerable<KeyValuePair<int, int>> source =
            Enumerable.Range(1, 5).Select(i => new KeyValuePair<int, int>(i, 100 - i));

        var queue = new IndexedPriorityQueue<int, int>(source);

        Assert.AreEqual(5, queue.Count);
        Assert.AreEqual(5, queue.Peek().Key); // smallest priority is element 5 with priority 95
    }
}

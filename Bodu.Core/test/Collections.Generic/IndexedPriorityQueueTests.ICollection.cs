// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.ICollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that <c>CopyTo</c> with a multidimensional destination throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[2, 2], 0);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> with a <see langword="null" /> destination throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            collection.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> with a destination element type that is incompatible with
    /// <see cref="KeyValuePair{TKey, TValue}" /> throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDestinationElementTypeIsIncompatible_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        ICollection collection = queue;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new int[8], 0);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> writes element-priority pairs into a destination typed as
    /// <see cref="object" /> (the non-generic <c>SetValue</c> branch).
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDestinationIsObjectArray_ShouldCopyAllPairs()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        var destination = new object?[2];
        collection.CopyTo(destination, 0);

        Assert.IsInstanceOfType<KeyValuePair<string, int>>(destination[0]);
        Assert.IsInstanceOfType<KeyValuePair<string, int>>(destination[1]);
        var keys = destination
            .Cast<KeyValuePair<string, int>>()
            .Select(p => p.Key)
            .ToArray();
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> writes element-priority pairs into a typed destination array.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDestinationIsTypedArray_ShouldCopyAllPairs()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        var destination = new KeyValuePair<string, int>[3];
        collection.CopyTo(destination, 1);

        Assert.AreEqual(default(KeyValuePair<string, int>), destination[0]);
        Assert.IsTrue(destination[1].Key is "a" or "b");
        Assert.IsTrue(destination[2].Key is "a" or "b");
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> on a typed destination writes every element-priority pair exactly once.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDestinationIsTypedArray_ShouldWriteEveryPair()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 6; i++)
            queue.Enqueue(i, 100 - i);
        ICollection collection = queue;

        var destination = new KeyValuePair<int, int>[6];
        collection.CopyTo(destination, 0);

        CollectionAssert.AreEquivalent(
            Enumerable.Range(0, 6).ToArray(),
            destination.Select(p => p.Key).ToArray());
        CollectionAssert.AreEquivalent(
            Enumerable.Range(0, 6).Select(i => 100 - i).ToArray(),
            destination.Select(p => p.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> into an array that is too small throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[1], 0);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> with a negative starting index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[1], -1);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> with an index that is in-range but leaves no room for the queue's
    /// elements throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexLeavesInsufficientRoom_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[3], 2);
        });
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> on an empty queue writes nothing and does not throw.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenQueueIsEmpty_ShouldNotThrow()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        var destination = new KeyValuePair<string, int>[2];
        destination[0] = new KeyValuePair<string, int>("sentinel", 42);
        collection.CopyTo(destination, 1);

        Assert.AreEqual("sentinel", destination[0].Key);
        Assert.AreEqual(default, destination[1]);
    }

    /// <summary>
    /// Verifies that <c>Count</c> on the <see cref="ICollection" /> facet matches the queue's count.
    /// </summary>
    [TestMethod]
    public void ICollection_Count_ShouldMatchQueueCount()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        Assert.HasCount(2, collection);
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerable{T}" /> exposes a per-call enumerator that does not interfere
    /// with concurrent in-flight enumerators.
    /// </summary>
    [TestMethod]
    public void ICollection_GetEnumerator_WhenInvokedTwice_ShouldReturnIndependentEnumerators()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 4; i++)
            queue.Enqueue(i, i);

        IEnumerable<KeyValuePair<int, int>> enumerable = queue;
        using IEnumerator<KeyValuePair<int, int>> first = enumerable.GetEnumerator();
        using IEnumerator<KeyValuePair<int, int>> second = enumerable.GetEnumerator();

        Assert.IsTrue(first.MoveNext());
        Assert.IsTrue(second.MoveNext());

        // Both enumerators independently observe the first element.
        Assert.AreEqual(first.Current.Key, second.Current.Key);
    }
    /// <summary>
    /// Verifies that <c>ICollection.IsSynchronized</c> is always <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ICollection_IsSynchronized_ShouldReturnFalse()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that <c>ICollection.SyncRoot</c> returns a stable, non-null object across calls.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldReturnStableNonNullObject()
    {
        ICollection collection = new IndexedPriorityQueue<string, int>();

        var first = collection.SyncRoot;
        var second = collection.SyncRoot;

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }

}

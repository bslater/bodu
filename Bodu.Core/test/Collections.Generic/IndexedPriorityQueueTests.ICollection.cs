// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
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

        object first = collection.SyncRoot;
        object second = collection.SyncRoot;

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
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
    /// Verifies that <c>Count</c> on the <see cref="ICollection" /> facet matches the queue's count.
    /// </summary>
    [TestMethod]
    public void ICollection_Count_ShouldMatchQueueCount()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        ICollection collection = queue;

        Assert.AreEqual(2, collection.Count);
    }
}

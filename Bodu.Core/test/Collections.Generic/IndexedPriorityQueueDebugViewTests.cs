// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueDebugViewTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public class IndexedPriorityQueueDebugViewTests
{

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> when given a null queue.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenQueueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new IndexedPriorityQueueDebugView<string, int>(null!);
        });
    }

    /// <summary>
    /// Verifies that the debug view does not mutate the queue.
    /// </summary>
    [TestMethod]
    public void Items_WhenAccessed_ShouldNotMutateQueue()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);

        var view = new IndexedPriorityQueueDebugView<string, int>(queue);
        _ = view.Items;

        Assert.AreEqual(2, queue.Count);
        Assert.IsTrue(queue.Contains("a"));
        Assert.IsTrue(queue.Contains("b"));
    }

    /// <summary>
    /// Verifies that the debug view exposes items sorted ascending by priority.
    /// </summary>
    [TestMethod]
    public void Items_WhenQueueHasMultipleEntries_ShouldReturnPriorityOrderedArray()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 30);
        queue.Enqueue("b", 10);
        queue.Enqueue("c", 20);

        var view = new IndexedPriorityQueueDebugView<string, int>(queue);
        KeyValuePair<string, int>[] items = view.Items;

        Assert.AreEqual(3, items.Length);
        Assert.AreEqual("b", items[0].Key);
        Assert.AreEqual("c", items[1].Key);
        Assert.AreEqual("a", items[2].Key);
    }

    /// <summary>
    /// Verifies that the debug view returns an empty array when the queue is empty.
    /// </summary>
    [TestMethod]
    public void Items_WhenQueueIsEmpty_ShouldReturnEmptyArray()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        var view = new IndexedPriorityQueueDebugView<string, int>(queue);

        Assert.AreEqual(0, view.Items.Length);
    }

}

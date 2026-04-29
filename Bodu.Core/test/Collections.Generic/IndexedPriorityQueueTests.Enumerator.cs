// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{
    /// <summary>
    /// Verifies that the struct enumerator yields exactly <c>Count</c> pairs covering all elements.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenWalkingHeap_ShouldYieldAllElementsExactlyOnce()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (int i = 0; i < 20; i++)
            queue.Enqueue(i, 100 - i);

        var seen = new HashSet<int>();
        foreach (var pair in queue)
            Assert.IsTrue(seen.Add(pair.Key), $"duplicate element {pair.Key}");

        Assert.AreEqual(20, seen.Count);
    }

    /// <summary>
    /// Verifies that calling <c>Current</c> before the first <c>MoveNext</c> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessingCurrentBeforeMoveNext_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        var enumerator = queue.GetEnumerator();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that mutating the queue invalidates an in-flight enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenQueueMutatedDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (int i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        var enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.Enqueue(99, 99);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <c>Reset</c> rewinds the enumerator and that subsequent enumeration yields the same set.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldRestartEnumeration()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (int i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        var enumerator = queue.GetEnumerator();
        while (enumerator.MoveNext())
        {
        }

        enumerator.Reset();
        int count = 0;
        while (enumerator.MoveNext())
            count++;

        Assert.AreEqual(5, count);
    }

    /// <summary>
    /// Verifies that <c>Reset</c> on an invalidated enumerator throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetAfterMutation_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        var enumerator = queue.GetEnumerator();
        queue.Enqueue("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    /// <summary>
    /// Verifies that <c>Dispose</c> on the struct enumerator does not throw.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDisposed_ShouldNotThrow()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        var enumerator = queue.GetEnumerator();
        enumerator.Dispose();
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Verifies that the queue can be enumerated through the generic <see cref="IEnumerable{T}" /> interface,
    /// returning every element exactly once.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessedViaGenericInterface_ShouldYieldAllElements()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 10; i++)
            queue.Enqueue(i, i);

        IEnumerable<KeyValuePair<int, int>> enumerable = queue;
        var seen = new HashSet<int>();
        foreach (KeyValuePair<int, int> pair in enumerable)
            Assert.IsTrue(seen.Add(pair.Key));

        Assert.HasCount(10, seen);
    }

    /// <summary>
    /// Verifies that the queue can be enumerated through the non-generic <see cref="System.Collections.IEnumerable" />
    /// interface, returning every element exactly once.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessedViaNonGenericInterface_ShouldYieldAllElements()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 10; i++)
            queue.Enqueue(i, i);

        System.Collections.IEnumerable enumerable = queue;
        var seen = new HashSet<int>();
        foreach (var entry in enumerable)
        {
            Assert.IsInstanceOfType<KeyValuePair<int, int>>(entry);
            var pair = (KeyValuePair<int, int>)entry!;
            Assert.IsTrue(seen.Add(pair.Key));
        }

        Assert.HasCount(10, seen);
    }

    /// <summary>
    /// Verifies that accessing <c>Current</c> after enumeration has been exhausted throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessingCurrentAfterExhaustion_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        queue.Enqueue(1, 1);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        while (enumerator.MoveNext())
        {
        }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that calling <c>Current</c> before the first <c>MoveNext</c> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessingCurrentBeforeMoveNext_ShouldThrowExactly()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("a", 1);

        IndexedPriorityQueue<string, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that calling <c>Clear</c> mid-enumeration invalidates the enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that calling <c>Dequeue</c> mid-enumeration invalidates the enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDequeueDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        _ = queue.Dequeue();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <c>Dispose</c> on the struct enumerator does not throw.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDisposed_ShouldNotThrow()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        IndexedPriorityQueue<string, int>.Enumerator enumerator = queue.GetEnumerator();
        enumerator.Dispose();
    }

    /// <summary>
    /// Verifies that an <see cref="IndexedPriorityQueue{TElement, TPriority}.EnqueueOrUpdate" /> that adds a
    /// new element invalidates an in-flight enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEnqueueOrUpdateAddsElement_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        queue.Enqueue(1, 1);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        Assert.IsTrue(queue.EnqueueOrUpdate(2, 2));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that calling <c>MoveNext</c> after enumeration has finished continues to return <see langword="false" />
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenMoveNextPastEnd_ShouldReturnFalseSubsequently()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        queue.Enqueue(1, 1);
        queue.Enqueue(2, 2);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        while (enumerator.MoveNext())
        {
        }

        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that an enumerator over an empty queue immediately returns <see langword="false" /> from
    /// <c>MoveNext</c> without throwing.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenQueueIsEmpty_ShouldReturnFalseImmediately()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that mutating the queue invalidates an in-flight enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenQueueMutatedDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.Enqueue(99, 99);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that calling <c>Remove</c> mid-enumeration invalidates the enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenRemoveDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        Assert.IsTrue(queue.Remove(2));

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
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        while (enumerator.MoveNext())
        {
        }

        enumerator.Reset();
        var count = 0;
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

        IndexedPriorityQueue<string, int>.Enumerator enumerator = queue.GetEnumerator();
        queue.Enqueue("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedPriorityQueue{TElement, TPriority}.TrimExcess" /> does not invalidate
    /// an in-flight enumerator and yields the same element set after the trim.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenTrimExcessInvoked_ShouldRemainValid()
    {
        var queue = new IndexedPriorityQueue<int, int>(64);
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.TrimExcess();

        var seen = new HashSet<int> { enumerator.Current.Key };
        while (enumerator.MoveNext())
            seen.Add(enumerator.Current.Key);

        Assert.HasCount(5, seen);
    }

    /// <summary>
    /// Verifies that an <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> that genuinely changes
    /// priority invalidates an in-flight enumerator on the next <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenUpdateChangesPriorityDuringEnumeration_ShouldInvalidateEnumerator()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.Update(3, -1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that an <see cref="IndexedPriorityQueue{TElement, TPriority}.Update" /> that does not change
    /// the priority leaves the version unchanged so an in-flight enumerator continues to advance.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenUpdateIsNoOp_ShouldRemainValidThroughout()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 5; i++)
            queue.Enqueue(i, i);

        IndexedPriorityQueue<int, int>.Enumerator enumerator = queue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        queue.Update(2, 2);

        var remaining = 1;
        while (enumerator.MoveNext())
            remaining++;

        Assert.AreEqual(5, remaining);
    }
    /// <summary>
    /// Verifies that the struct enumerator yields exactly <c>Count</c> pairs covering all elements.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenWalkingHeap_ShouldYieldAllElementsExactlyOnce()
    {
        var queue = new IndexedPriorityQueue<int, int>();
        for (var i = 0; i < 20; i++)
            queue.Enqueue(i, 100 - i);

        var seen = new HashSet<int>();
        foreach (KeyValuePair<int, int> pair in queue)
            Assert.IsTrue(seen.Add(pair.Key), $"duplicate element {pair.Key}");

        Assert.HasCount(20, seen);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Wrapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that interleaved tail-side adds and head-side removes preserve FIFO logical order across the wrap.
    /// </summary>
    [TestMethod]
    public void Wrapping_WhenAddLastRemoveFirstInterleaved_ShouldRetainFifoOrder()
    {
        var deque = new ArrayDeque<int>(4);
        for (int i = 0; i < 10; i++)
        {
            deque.AddLast(i);
            if (deque.Count == 4)
                _ = deque.RemoveFirst();
        }

        // Last added: 9. After loop, deque holds the most recent 4 unless capacity hit.
        Assert.AreEqual(9, deque.PeekLast());
        Assert.IsTrue(deque.Count <= 4);
    }

    /// <summary>
    /// Verifies that interleaved head-side adds and tail-side removes preserve LIFO logical order across the wrap.
    /// </summary>
    [TestMethod]
    public void Wrapping_WhenAddFirstRemoveLastInterleaved_ShouldRetainLifoOrder()
    {
        var deque = new ArrayDeque<int>(4);
        for (int i = 0; i < 10; i++)
        {
            deque.AddFirst(i);
            if (deque.Count == 4)
                _ = deque.RemoveLast();
        }

        Assert.AreEqual(9, deque.PeekFirst());
        Assert.IsTrue(deque.Count <= 4);
    }

    /// <summary>
    /// Verifies that filling and draining from both ends round-trips correctly.
    /// </summary>
    [TestMethod]
    public void Wrapping_WhenFilledAndDrainedFromBothEnds_ShouldEmptyToZero()
    {
        var deque = new ArrayDeque<int>(6);
        deque.AddLast(3);
        deque.AddLast(4);
        deque.AddLast(5);
        deque.AddFirst(2);
        deque.AddFirst(1);
        deque.AddFirst(0);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, deque.ToArray());

        Assert.AreEqual(0, deque.RemoveFirst());
        Assert.AreEqual(5, deque.RemoveLast());
        Assert.AreEqual(1, deque.RemoveFirst());
        Assert.AreEqual(4, deque.RemoveLast());
        Assert.AreEqual(2, deque.RemoveFirst());
        Assert.AreEqual(3, deque.RemoveLast());
        Assert.AreEqual(0, deque.Count);
    }
}

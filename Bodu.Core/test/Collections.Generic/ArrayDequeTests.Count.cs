// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Count"/> tracks adds at both ends.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAddedAtBothEnds_ShouldReflectTotal()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(1);
        deque.AddFirst(2);
        deque.AddLast(3);
        Assert.AreEqual(3, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Count"/> tracks removes at both ends.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsRemovedFromBothEnds_ShouldReflectTotal()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        _ = deque.RemoveLast();
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsEmpty"/> tracks the deque state.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenItemsRemoved_ShouldBecomeTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        Assert.IsFalse(deque.IsEmpty);
        _ = deque.RemoveFirst();
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsFull"/> tracks the deque state.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenAtCapacity_ShouldBecomeTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        Assert.IsFalse(deque.IsFull);
        deque.AddLast(2);
        Assert.IsTrue(deque.IsFull);
    }
}

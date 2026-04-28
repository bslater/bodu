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
    /// Verifies that <see cref="ArrayDeque{T}.Count"/> is zero on a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewlyConstructed_ShouldBeZero()
    {
        var deque = new ArrayDeque<int>(5);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Count"/> resets to zero after <see cref="ArrayDeque{T}.Clear"/>.
    /// </summary>
    [TestMethod]
    public void Count_AfterClear_ShouldBeZero()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.AreEqual(0, deque.Count);
    }
}

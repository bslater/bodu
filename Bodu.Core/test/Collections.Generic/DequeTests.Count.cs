// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.Count"/> is zero on a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewlyConstructed_ShouldBeZero()
    {
        var deque = new Deque<int>();
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Count"/> tracks adds at both ends.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAddedAtBothEnds_ShouldReflectTotal()
    {
        var deque = new Deque<int>(5);
        deque.AddLast(1);
        deque.AddFirst(2);
        deque.AddLast(3);
        Assert.AreEqual(3, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Count"/> tracks removes at both ends.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsRemovedFromBothEnds_ShouldReflectTotal()
    {
        var deque = new Deque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        _ = deque.RemoveLast();
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Count"/> remains accurate across an auto-grow event.
    /// </summary>
    [TestMethod]
    public void Count_WhenAutoGrows_ShouldReflectTotal()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 50; i++)
            deque.AddLast(i);
        Assert.AreEqual(50, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Count"/> resets to zero after <see cref="Deque{T}.Clear"/>.
    /// </summary>
    [TestMethod]
    public void Count_AfterClear_ShouldBeZero()
    {
        var deque = new Deque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.AreEqual(0, deque.Count);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.IsFull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsFull"/> is <see langword="false"/> for a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenNewlyConstructed_ShouldBeFalse()
    {
        var deque = new ArrayDeque<int>(3);
        Assert.IsFalse(deque.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsFull"/> becomes <see langword="true"/> when <see cref="ArrayDeque{T}.Count"/> equals <see cref="ArrayDeque{T}.Capacity"/>.
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

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsFull"/> reverts to <see langword="false"/> after a removal.
    /// </summary>
    [TestMethod]
    public void IsFull_AfterRemoval_ShouldBeFalse()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.IsTrue(deque.IsFull);

        _ = deque.RemoveFirst();
        Assert.IsFalse(deque.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsFull"/> reflects fullness reached via head-side adds.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenFilledViaAddFirst_ShouldBecomeTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddFirst(1);
        deque.AddFirst(2);
        Assert.IsTrue(deque.IsFull);
    }
}

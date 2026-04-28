// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Clear"/> resets <see cref="ArrayDeque{T}.Count"/> to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDequeHasItems_ShouldResetCount()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.AreEqual(0, deque.Count);
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Clear"/> preserves <see cref="ArrayDeque{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDequeHasItems_ShouldPreserveCapacity()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(1);
        deque.Clear();
        Assert.AreEqual(5, deque.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Clear"/> permits new operations afterwards.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldAllowSubsequentOperations()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.Clear();
        deque.AddLast(99);
        Assert.AreEqual(99, deque.PeekFirst());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Clear"/> on an already-empty deque is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var deque = new ArrayDeque<int>(3);
        deque.Clear();
        Assert.AreEqual(0, deque.Count);
    }
}

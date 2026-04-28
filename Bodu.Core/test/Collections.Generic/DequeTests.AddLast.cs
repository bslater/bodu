// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.AddLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)"/> appends the new item at the tail.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenInvoked_ShouldAppendItem()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)"/> never throws on capacity — it auto-grows.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenCapacityReached_ShouldGrowAndAccept()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 100; i++)
            deque.AddLast(i);

        Assert.AreEqual(100, deque.Count);
        Assert.IsTrue(deque.Capacity >= 100);
        for (int i = 0; i < 100; i++)
            Assert.AreEqual(i, deque[i]);
    }
}

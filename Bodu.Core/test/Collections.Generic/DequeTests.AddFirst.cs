// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.AddFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)"/> places the new item at the head.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenInvoked_ShouldPlaceItemAtHead()
    {
        var deque = new Deque<int>(3);
        deque.AddFirst(1);
        deque.AddFirst(2);
        deque.AddFirst(3);
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)"/> never throws on capacity — it auto-grows.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenCapacityReached_ShouldGrowAndAccept()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 100; i++)
            deque.AddFirst(i);

        Assert.AreEqual(100, deque.Count);
        Assert.IsTrue(deque.Capacity >= 100);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenNullProvidedForReferenceType_ShouldAccept()
    {
        var deque = new Deque<string?>(2);
        deque.AddFirst(null);
        Assert.IsNull(deque.PeekFirst());
    }
}

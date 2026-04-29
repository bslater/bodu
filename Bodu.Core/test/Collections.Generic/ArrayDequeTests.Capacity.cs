// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Capacity"/> reflects the requested capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenCustomCapacityProvided_ShouldReturnSpecifiedValue()
    {
        var deque = new ArrayDeque<int>(7);
        Assert.AreEqual(7, deque.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Capacity"/> does not change after adds and removes.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenItemsAddedAndRemoved_ShouldRemainConstant()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        _ = deque.RemoveFirst();
        Assert.AreEqual(5, deque.Capacity);
    }
}

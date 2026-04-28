// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.ToArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.ToArray"/> returns elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenMixedInsertions_ShouldReturnHeadToTailOrder()
    {
        var deque = new Deque<int>(5);
        deque.AddLast(2);
        deque.AddLast(3);
        deque.AddFirst(1);
        deque.AddLast(4);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.ToArray"/> returns an empty array for an empty deque.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenEmpty_ShouldReturnEmptyArray()
    {
        var deque = new Deque<int>();
        Assert.AreEqual(0, deque.ToArray().Length);
    }
}

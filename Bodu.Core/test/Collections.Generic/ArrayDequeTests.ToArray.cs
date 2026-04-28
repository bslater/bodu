// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.ToArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.ToArray"/> returns a new array of the correct length.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDequeHasItems_ShouldReturnArrayOfCorrectLength()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.AreEqual(2, deque.ToArray().Length);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.ToArray"/> returns an empty array when the deque is empty.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDequeIsEmpty_ShouldReturnEmptyArray()
    {
        var deque = new ArrayDeque<int>(3);
        Assert.AreEqual(0, deque.ToArray().Length);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.ToArray"/> reflects head-to-tail order with mixed insertions.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenMixedInsertions_ShouldReturnHeadToTailOrder()
    {
        var deque = new ArrayDeque<int>(5);
        deque.AddLast(2);
        deque.AddLast(3);
        deque.AddFirst(1);
        deque.AddLast(4);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that the returned array is independent of the deque's backing storage.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnIndependentCopy()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        var copy = deque.ToArray();
        deque.AddLast(2);
        Assert.AreEqual(1, copy.Length);
    }
}

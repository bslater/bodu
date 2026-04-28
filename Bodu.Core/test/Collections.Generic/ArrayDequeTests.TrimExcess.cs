// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.TrimExcess.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TrimExcess"/> reduces capacity to match <see cref="ArrayDeque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenDequeHasItems_ShouldShrinkCapacityToCount()
    {
        var deque = new ArrayDeque<int>(100);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.TrimExcess();
        Assert.AreEqual(2, deque.Capacity);
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TrimExcess"/> keeps the elements available in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenStorageWrapped_ShouldPreserveOrder()
    {
        var deque = new ArrayDeque<int>(4);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        deque.AddLast(4);
        _ = deque.RemoveFirst();
        _ = deque.RemoveFirst();
        deque.AddLast(5);
        deque.AddLast(6);

        deque.TrimExcess();
        CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TrimExcess"/> on an empty deque shrinks the capacity to one.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenDequeIsEmpty_ShouldShrinkToOne()
    {
        var deque = new ArrayDeque<int>(50);
        deque.TrimExcess();
        Assert.AreEqual(1, deque.Capacity);
    }
}

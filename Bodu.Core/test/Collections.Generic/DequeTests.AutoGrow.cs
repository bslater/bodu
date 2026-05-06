// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.AutoGrow.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that filling beyond initial capacity preserves logical order from tail-side adds.
    /// </summary>
    [TestMethod]
    public void AutoGrow_WhenAddLastBeyondCapacity_ShouldPreserveOrder()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 50; i++)
            deque.AddLast(i);

        Assert.AreEqual(50, deque.Count);
        for (int i = 0; i < 50; i++)
            Assert.AreEqual(i, deque[i]);
    }

    /// <summary>
    /// Verifies that filling beyond initial capacity preserves logical order from head-side adds.
    /// </summary>
    [TestMethod]
    public void AutoGrow_WhenAddFirstBeyondCapacity_ShouldPreserveOrder()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 50; i++)
            deque.AddFirst(i);

        Assert.AreEqual(50, deque.Count);
        for (int i = 0; i < 50; i++)
            Assert.AreEqual(49 - i, deque[i]);
    }

    /// <summary>
    /// Verifies that growth across a wrapped layout retains logical order.
    /// </summary>
    [TestMethod]
    public void AutoGrow_WhenWrappedThenGrown_ShouldPreserveOrder()
    {
        var deque = new Deque<int>(4);
        deque.AddLast(0);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        _ = deque.RemoveFirst();
        deque.AddLast(4);
        deque.AddLast(5); // wrapped, full
        deque.AddLast(6); // triggers grow

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, deque.ToArray());
    }
}

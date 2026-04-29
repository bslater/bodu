// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TrimExcess.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.TrimExcess"/> shrinks capacity to <see cref="Deque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalled_ShouldShrinkCapacityToCount()
    {
        var deque = new Deque<int>(100);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.TrimExcess();
        Assert.AreEqual(2, deque.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TrimExcess"/> on an empty deque shrinks capacity to one.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenEmpty_ShouldShrinkToOne()
    {
        var deque = new Deque<int>(100);
        deque.TrimExcess();
        Assert.AreEqual(1, deque.Capacity);
    }

    /// <summary>
    /// Verifies that subsequent operations after trimming work correctly and grow as needed.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalledThenAddLast_ShouldGrowAgain()
    {
        var deque = new Deque<int>(100);
        deque.AddLast(1);
        deque.TrimExcess();
        Assert.AreEqual(1, deque.Capacity);

        deque.AddLast(2);
        Assert.AreEqual(2, deque.Count);
        Assert.IsTrue(deque.Capacity >= 2);
    }
}

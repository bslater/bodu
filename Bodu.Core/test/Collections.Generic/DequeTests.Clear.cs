// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.Clear"/> resets <see cref="Deque{T}.Count"/> to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDequeHasItems_ShouldResetCount()
    {
        var deque = new Deque<int>(5);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.AreEqual(0, deque.Count);
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Clear"/> preserves <see cref="Deque{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldPreserveCapacity()
    {
        var deque = new Deque<int>(50);
        deque.AddLast(1);
        deque.Clear();
        Assert.IsTrue(deque.Capacity >= 50);
    }
}

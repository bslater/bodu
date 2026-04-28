// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.RemoveFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveFirst"/> returns and removes the head element.
    /// </summary>
    [TestMethod]
    public void RemoveFirst_WhenDequeHasItems_ShouldReturnAndRemoveHead()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(1, deque.RemoveFirst());
        Assert.AreEqual(1, deque.Count);
        Assert.AreEqual(2, deque.PeekFirst());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveFirst"/> drains the deque to empty when called repeatedly.
    /// </summary>
    [TestMethod]
    public void RemoveFirst_WhenCalledRepeatedly_ShouldDrainToEmpty()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        Assert.AreEqual(1, deque.RemoveFirst());
        Assert.AreEqual(2, deque.RemoveFirst());
        Assert.AreEqual(3, deque.RemoveFirst());
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveFirst"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void RemoveFirst_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new Deque<int>();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.RemoveFirst();
        });
    }
}

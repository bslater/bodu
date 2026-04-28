// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.RemoveLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveLast"/> returns and removes the tail element.
    /// </summary>
    [TestMethod]
    public void RemoveLast_WhenDequeHasItems_ShouldReturnAndRemoveTail()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(2, deque.RemoveLast());
        Assert.AreEqual(1, deque.Count);
        Assert.AreEqual(1, deque.PeekLast());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveLast"/> drains the deque to empty when called repeatedly.
    /// </summary>
    [TestMethod]
    public void RemoveLast_WhenCalledRepeatedly_ShouldDrainToEmpty()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        Assert.AreEqual(3, deque.RemoveLast());
        Assert.AreEqual(2, deque.RemoveLast());
        Assert.AreEqual(1, deque.RemoveLast());
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.RemoveLast"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void RemoveLast_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new Deque<int>();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.RemoveLast();
        });
    }
}

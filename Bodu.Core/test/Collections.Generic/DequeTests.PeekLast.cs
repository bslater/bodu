// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.PeekLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.PeekLast"/> returns the tail without removing it.
    /// </summary>
    [TestMethod]
    public void PeekLast_WhenDequeHasItems_ShouldReturnTailWithoutRemoving()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.AreEqual(2, deque.PeekLast());
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.PeekLast"/> reflects the new tail after the previous tail is removed.
    /// </summary>
    [TestMethod]
    public void PeekLast_AfterRemoveLast_ShouldReturnNewTail()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        _ = deque.RemoveLast();
        Assert.AreEqual(1, deque.PeekLast());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.PeekLast"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void PeekLast_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new Deque<int>();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.PeekLast();
        });
    }
}

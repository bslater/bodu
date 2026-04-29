// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.PeekFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.PeekFirst"/> returns the head without removing it.
    /// </summary>
    [TestMethod]
    public void PeekFirst_WhenDequeHasItems_ShouldReturnHeadWithoutRemoving()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(1, deque.PeekFirst());
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.PeekFirst"/> reflects the new head after the previous head is removed.
    /// </summary>
    [TestMethod]
    public void PeekFirst_AfterRemoveFirst_ShouldReturnNewHead()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        _ = deque.RemoveFirst();
        Assert.AreEqual(2, deque.PeekFirst());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.PeekFirst"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void PeekFirst_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.PeekFirst();
        });
    }
}

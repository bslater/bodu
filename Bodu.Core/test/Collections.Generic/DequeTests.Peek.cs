// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Peek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.PeekFirst"/> returns the head without removing it.
    /// </summary>
    [TestMethod]
    public void PeekFirst_WhenDequeHasItems_ShouldReturnHeadWithoutRemoving()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.AreEqual(1, deque.PeekFirst());
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.PeekFirst"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void PeekFirst_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new Deque<int>();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.PeekFirst();
        });
    }

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

    /// <summary>
    /// Verifies that the <c>TryPeek*</c> variants return false when the deque is empty.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new Deque<int>();
        Assert.IsFalse(deque.TryPeekFirst(out _));
        Assert.IsFalse(deque.TryPeekLast(out _));
    }
}

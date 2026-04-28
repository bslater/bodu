// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Peek.cs" company="PlaceholderCompany">
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

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.PeekLast"/> returns the tail without removing it.
    /// </summary>
    [TestMethod]
    public void PeekLast_WhenDequeHasItems_ShouldReturnTailWithoutRemoving()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(2, deque.PeekLast());
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.PeekLast"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void PeekLast_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.PeekLast();
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryPeekFirst(out T)"/> succeeds when the deque has items.
    /// </summary>
    [TestMethod]
    public void TryPeekFirst_WhenDequeHasItems_ShouldReturnTrueAndHead()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(7);
        Assert.IsTrue(deque.TryPeekFirst(out int item));
        Assert.AreEqual(7, item);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryPeekFirst(out T)"/> returns <see langword="false"/> when empty.
    /// </summary>
    [TestMethod]
    public void TryPeekFirst_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.TryPeekFirst(out int item));
        Assert.AreEqual(default, item);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryPeekLast(out T)"/> succeeds when the deque has items.
    /// </summary>
    [TestMethod]
    public void TryPeekLast_WhenDequeHasItems_ShouldReturnTrueAndTail()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(7);
        deque.AddLast(8);
        Assert.IsTrue(deque.TryPeekLast(out int item));
        Assert.AreEqual(8, item);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryPeekLast(out T)"/> returns <see langword="false"/> when empty.
    /// </summary>
    [TestMethod]
    public void TryPeekLast_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.TryPeekLast(out int item));
        Assert.AreEqual(default, item);
    }
}

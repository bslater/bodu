// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.RemoveLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveLast"/> returns and removes the tail element.
    /// </summary>
    [TestMethod]
    public void RemoveLast_WhenDequeHasItems_ShouldReturnAndRemoveTail()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(2, deque.RemoveLast());
        Assert.AreEqual(1, deque.Count);
        Assert.AreEqual(1, deque.PeekLast());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveLast"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void RemoveLast_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.RemoveLast();
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryRemoveLast(out T)"/> returns the tail and <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenDequeHasItems_ShouldReturnTailAndTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(10);

        Assert.IsTrue(deque.TryRemoveLast(out int item));
        Assert.AreEqual(10, item);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryRemoveLast(out T)"/> returns <see langword="false"/> when the deque is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.TryRemoveLast(out int item));
        Assert.AreEqual(default, item);
    }
}

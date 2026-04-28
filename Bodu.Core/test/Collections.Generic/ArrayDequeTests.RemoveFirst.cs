// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.RemoveFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveFirst"/> returns and removes the head element.
    /// </summary>
    [TestMethod]
    public void RemoveFirst_WhenDequeHasItems_ShouldReturnAndRemoveHead()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.AreEqual(1, deque.RemoveFirst());
        Assert.AreEqual(1, deque.Count);
        Assert.AreEqual(2, deque.PeekFirst());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveFirst"/> throws when the deque is empty.
    /// </summary>
    [TestMethod]
    public void RemoveFirst_WhenEmpty_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = deque.RemoveFirst();
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryRemoveFirst(out T)"/> returns the head and <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenDequeHasItems_ShouldReturnHeadAndTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(10);

        Assert.IsTrue(deque.TryRemoveFirst(out int item));
        Assert.AreEqual(10, item);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryRemoveFirst(out T)"/> returns <see langword="false"/> when the deque is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.TryRemoveFirst(out int item));
        Assert.AreEqual(default, item);
    }
}

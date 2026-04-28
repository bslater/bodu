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
    /// Verifies that <see cref="Deque{T}.RemoveFirst"/> throws when empty.
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

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveFirst(out T)"/> returns the head and <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenDequeHasItems_ShouldReturnHeadAndTrue()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(7);
        Assert.IsTrue(deque.TryRemoveFirst(out int item));
        Assert.AreEqual(7, item);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveFirst(out T)"/> returns <see langword="false"/> when empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new Deque<int>();
        Assert.IsFalse(deque.TryRemoveFirst(out _));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TryRemoveLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveLast(out T)"/> returns the tail and <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenDequeHasItems_ShouldReturnTailAndTrue()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(10);

        Assert.IsTrue(deque.TryRemoveLast(out int item));
        Assert.AreEqual(10, item);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveLast(out T)"/> returns <see langword="false"/> and the default value when the deque is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new Deque<int>();
        Assert.IsFalse(deque.TryRemoveLast(out int item));
        Assert.AreEqual(default, item);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveLast(out T)"/> does not modify state when it returns <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenEmpty_ShouldNotModifyState()
    {
        var deque = new Deque<int>(8);
        int capacityBefore = deque.Capacity;
        _ = deque.TryRemoveLast(out _);
        Assert.IsTrue(deque.IsEmpty);
        Assert.AreEqual(capacityBefore, deque.Capacity);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TryRemoveFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
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
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveFirst(out T)"/> returns <see langword="false"/> and the default value when the deque is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new Deque<int>();
        Assert.IsFalse(deque.TryRemoveFirst(out int item));
        Assert.AreEqual(default, item);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryRemoveFirst(out T)"/> does not modify state when it returns <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenEmpty_ShouldNotModifyState()
    {
        var deque = new Deque<int>(8);
        int capacityBefore = deque.Capacity;
        _ = deque.TryRemoveFirst(out _);
        Assert.IsTrue(deque.IsEmpty);
        Assert.AreEqual(capacityBefore, deque.Capacity);
    }
}

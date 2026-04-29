// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TryPeekFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryPeekFirst(out T)"/> succeeds when the deque has items.
    /// </summary>
    [TestMethod]
    public void TryPeekFirst_WhenDequeHasItems_ShouldReturnTrueAndHead()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(7);
        Assert.IsTrue(deque.TryPeekFirst(out int item));
        Assert.AreEqual(7, item);
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryPeekFirst(out T)"/> returns <see langword="false"/> and the default value when empty.
    /// </summary>
    [TestMethod]
    public void TryPeekFirst_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new Deque<int>();
        Assert.IsFalse(deque.TryPeekFirst(out int item));
        Assert.AreEqual(default, item);
    }
}

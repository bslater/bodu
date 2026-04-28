// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.TryPeekLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
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
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryPeekLast(out T)"/> returns <see langword="false"/> and the default value when empty.
    /// </summary>
    [TestMethod]
    public void TryPeekLast_WhenEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.TryPeekLast(out int item));
        Assert.AreEqual(default, item);
    }
}

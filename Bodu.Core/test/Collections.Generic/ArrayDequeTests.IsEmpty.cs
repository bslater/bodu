// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.IsEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsEmpty"/> is <see langword="true"/> for a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenNewlyConstructed_ShouldBeTrue()
    {
        var deque = new ArrayDeque<int>(3);
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsEmpty"/> becomes <see langword="false"/> after an item is added.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenItemAdded_ShouldBecomeFalse()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        Assert.IsFalse(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsEmpty"/> returns <see langword="true"/> again after the last item is removed.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenAllItemsRemoved_ShouldBecomeTrue()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        _ = deque.RemoveFirst();
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.IsEmpty"/> returns <see langword="true"/> after <see cref="ArrayDeque{T}.Clear"/>.
    /// </summary>
    [TestMethod]
    public void IsEmpty_AfterClear_ShouldBeTrue()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.IsTrue(deque.IsEmpty);
    }
}

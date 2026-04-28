// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.IsEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsEmpty"/> is <see langword="true"/> for a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenNewlyConstructed_ShouldBeTrue()
    {
        var deque = new Deque<int>();
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsEmpty"/> becomes <see langword="false"/> after an item is added.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenItemAdded_ShouldBecomeFalse()
    {
        var deque = new Deque<int>();
        deque.AddLast(1);
        Assert.IsFalse(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsEmpty"/> returns <see langword="true"/> again after the last item is removed.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenAllItemsRemoved_ShouldBecomeTrue()
    {
        var deque = new Deque<int>();
        deque.AddLast(1);
        _ = deque.RemoveFirst();
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsEmpty"/> returns <see langword="true"/> after <see cref="Deque{T}.Clear"/>.
    /// </summary>
    [TestMethod]
    public void IsEmpty_AfterClear_ShouldBeTrue()
    {
        var deque = new Deque<int>();
        deque.AddLast(1);
        deque.AddLast(2);
        deque.Clear();
        Assert.IsTrue(deque.IsEmpty);
    }
}

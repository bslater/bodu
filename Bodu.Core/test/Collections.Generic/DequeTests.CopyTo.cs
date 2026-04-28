// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.CopyTo(T[], int)"/> copies in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDequeHasItems_ShouldCopyInOrder()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        var target = new int[3];
        deque.CopyTo(target, 0);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.CopyTo(T[], int)"/> throws on null array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            deque.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.CopyTo(T[], int)"/> throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        var target = new int[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            deque.CopyTo(target, 0);
        });
    }
}

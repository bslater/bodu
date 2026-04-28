// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> copies elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDequeHasElements_ShouldCopyInOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        var target = new int[3];
        deque.CopyTo(target, 0);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> writes at the specified offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexNonZero_ShouldStartAtOffset()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);
        var target = new int[5];
        deque.CopyTo(target, 2);
        CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 0 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> handles wrapped storage correctly.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenStorageWrapped_ShouldCopyInLogicalOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        deque.AddLast(4);
        var target = new int[3];
        deque.CopyTo(target, 0);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> throws when given a null array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            deque.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> throws when the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        var target = new int[2];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            deque.CopyTo(target, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.CopyTo(T[], int)"/> throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(3);
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

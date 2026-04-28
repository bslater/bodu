// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Contains(T)"/> returns <see langword="true"/> when the item is present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsPresent_ShouldReturnTrue()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.IsTrue(deque.Contains(2));
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Contains(T)"/> returns <see langword="false"/> when the item is not present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNotPresent_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        Assert.IsFalse(deque.Contains(99));
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Contains(T)"/> returns <see langword="false"/> for an empty deque.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDequeIsEmpty_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(deque.Contains(0));
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Contains(T)"/> finds elements in the wrapped second segment.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStorageWrapped_ShouldFindItemInSecondSegment()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        deque.AddLast(4); // wraps

        Assert.IsTrue(deque.Contains(4));
        Assert.IsTrue(deque.Contains(2));
        Assert.IsFalse(deque.Contains(1));
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Contains(T)"/> handles <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void Contains_WhenNullProvidedForReferenceType_ShouldReturnTrueIfNullStored()
    {
        var deque = new ArrayDeque<string?>(3);
        deque.AddLast("a");
        deque.AddLast(null);
        Assert.IsTrue(deque.Contains(null));
    }
}

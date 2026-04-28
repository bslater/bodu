// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.TryAddFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddFirst(T)"/> returns <see langword="true"/> when there is space.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenSpaceAvailable_ShouldReturnTrueAndAdd()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsTrue(deque.TryAddFirst(1));
        Assert.AreEqual(1, deque.PeekFirst());
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddFirst(T)"/> returns <see langword="false"/> when the deque is full and does not modify state.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFull_ShouldReturnFalseAndPreserveState()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddFirst(1);
        deque.AddFirst(2);
        Assert.IsFalse(deque.TryAddFirst(3));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 2, 1 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddFirst(T)"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenNullProvidedForReferenceType_ShouldReturnTrue()
    {
        var deque = new ArrayDeque<string?>(2);
        Assert.IsTrue(deque.TryAddFirst(null));
        Assert.IsNull(deque.PeekFirst());
    }
}

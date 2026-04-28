// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.TryAddLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddLast(T)"/> returns <see langword="true"/> when there is space.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenSpaceAvailable_ShouldReturnTrueAndAdd()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsTrue(deque.TryAddLast(1));
        Assert.AreEqual(1, deque.PeekLast());
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddLast(T)"/> returns <see langword="false"/> when the deque is full and does not modify state.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenFull_ShouldReturnFalseAndPreserveState()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.IsFalse(deque.TryAddLast(3));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddLast(T)"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenNullProvidedForReferenceType_ShouldReturnTrue()
    {
        var deque = new ArrayDeque<string?>(2);
        Assert.IsTrue(deque.TryAddLast(null));
        Assert.IsNull(deque.PeekLast());
    }
}

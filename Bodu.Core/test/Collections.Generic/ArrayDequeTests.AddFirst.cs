// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.AddFirst.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddFirst(T)"/> places the new item at the head.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenDequeHasSpace_ShouldPlaceItemAtHead()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddFirst(1);
        deque.AddFirst(2);
        deque.AddFirst(3);
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddFirst(T)"/> increments <see cref="ArrayDeque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenItemAdded_ShouldIncrementCount()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddFirst(1);
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddFirst(T)"/> throws when the deque is full.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenFull_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddFirst(1);
        deque.AddFirst(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            deque.AddFirst(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddFirst(T)"/> returns <see langword="true"/> when there is space.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenSpaceAvailable_ShouldReturnTrue()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsTrue(deque.TryAddFirst(1));
        Assert.AreEqual(1, deque.PeekFirst());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.TryAddFirst(T)"/> returns <see langword="false"/> when the deque is full.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFull_ShouldReturnFalse()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddFirst(1);
        deque.AddFirst(2);
        Assert.IsFalse(deque.TryAddFirst(3));
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddFirst(T)"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenNullProvidedForReferenceType_ShouldAccept()
    {
        var deque = new ArrayDeque<string?>(2);
        deque.AddFirst(null);
        Assert.IsNull(deque.PeekFirst());
    }
}

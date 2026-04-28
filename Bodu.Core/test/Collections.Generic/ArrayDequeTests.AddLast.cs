// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.AddLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddLast(T)"/> appends the new item at the tail.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenDequeHasSpace_ShouldAppendItem()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddLast(T)"/> increments <see cref="ArrayDeque{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenItemAdded_ShouldIncrementCount()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        Assert.AreEqual(1, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddLast(T)"/> throws when the deque is full.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenFull_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            deque.AddLast(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddLast(T)"/> accepts duplicate values.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenDuplicatesProvided_ShouldStoreAll()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(7);
        deque.AddLast(7);
        deque.AddLast(7);
        CollectionAssert.AreEqual(new[] { 7, 7, 7 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.AddLast(T)"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenNullProvidedForReferenceType_ShouldAccept()
    {
        var deque = new ArrayDeque<string?>(2);
        deque.AddLast(null);
        Assert.IsNull(deque.PeekLast());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeDebugViewTests.Items.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeDebugViewTests
{
    /// <summary>
    /// Verifies that <c>Items</c> returns an empty array when the deque is empty.
    /// </summary>
    [TestMethod]
    public void Items_WhenDequeIsEmpty_ShouldReturnEmptyArray()
    {
        var deque = new ArrayDeque<string>(5);
        var view = new ArrayDequeDebugView<string>(deque);

        CollectionAssert.AreEqual(Array.Empty<string>(), view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> exposes the deque's elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Items_WhenDequeIsFilled_ShouldReturnItemsInHeadToTailOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(100);
        deque.AddLast(200);
        deque.AddLast(300);

        var view = new ArrayDequeDebugView<int>(deque);

        CollectionAssert.AreEqual(new[] { 100, 200, 300 }, view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> reflects logical order correctly after the storage has wrapped.
    /// </summary>
    [TestMethod]
    public void Items_WhenStorageWrapped_ShouldReturnItemsInLogicalOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        _ = deque.RemoveFirst();
        deque.AddLast(4); // wraps

        var view = new ArrayDequeDebugView<int>(deque);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> retains a reference to the deque even after the external reference is cleared.
    /// </summary>
    [TestMethod]
    public void Items_WhenOriginalDequeReferenceCleared_ShouldStillExposeContents()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var view = new ArrayDequeDebugView<int>(deque);

        deque = null; // simulate external loss of reference

        CollectionAssert.AreEqual(new[] { 1, 2 }, view.Items);
    }
}

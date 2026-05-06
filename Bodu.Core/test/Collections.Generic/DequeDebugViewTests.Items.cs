// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeDebugViewTests.Items.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeDebugViewTests
{
    /// <summary>
    /// Verifies that <c>Items</c> returns an empty array when the deque is empty.
    /// </summary>
    [TestMethod]
    public void Items_WhenDequeIsEmpty_ShouldReturnEmptyArray()
    {
        var deque = new Deque<string>(5);
        var view = new DequeDebugView<string>(deque);

        CollectionAssert.AreEqual(Array.Empty<string>(), view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> exposes the deque's elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Items_WhenDequeIsFilled_ShouldReturnItemsInHeadToTailOrder()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(100);
        deque.AddLast(200);
        deque.AddLast(300);

        var view = new DequeDebugView<int>(deque);

        CollectionAssert.AreEqual(new[] { 100, 200, 300 }, view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> reflects logical order correctly across an auto-grow event.
    /// </summary>
    [TestMethod]
    public void Items_WhenStorageGrew_ShouldReturnItemsInLogicalOrder()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 5; i++)
            deque.AddLast(i);

        var view = new DequeDebugView<int>(deque);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, view.Items);
    }

    /// <summary>
    /// Verifies that <c>Items</c> retains a reference to the deque even after the external reference is cleared.
    /// </summary>
    [TestMethod]
    public void Items_WhenOriginalDequeReferenceCleared_ShouldStillExposeContents()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var view = new DequeDebugView<int>(deque);

        deque = null; // simulate external loss of reference

        CollectionAssert.AreEqual(new[] { 1, 2 }, view.Items);
    }
}

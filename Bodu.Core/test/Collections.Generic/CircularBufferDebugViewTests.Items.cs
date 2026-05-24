// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferDebugViewTests.Items.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferDebugViewTests
{

    /// <summary>
    /// Verifies that DebugView returns an empty array when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void Items_WhenBufferIsEmpty_ShouldReturnEmptyArray()
    {
        var buffer = new CircularBuffer<string>(5);
        var view = new CircularBufferDebugView<string>(buffer);

        CollectionAssert.AreEqual(Array.Empty<string>(), view.Items);
    }

    /// <summary>
    /// Verifies that DebugView exposes buffer items in correct FirstInFirstOut order.
    /// </summary>
    [TestMethod]
    public void Items_WhenBufferIsFilled_ShouldReturnItemsInFIFOOrder()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(100);
        buffer.Enqueue(200);
        buffer.Enqueue(300);

        var view = new CircularBufferDebugView<int>(buffer);

        CollectionAssert.AreEqual(new[] { 100, 200, 300 }, view.Items);
    }

    /// <summary>
    /// Verifies that DebugView reflects item order correctly after circular buffer wraparound.
    /// </summary>
    [TestMethod]
    public void Items_WhenBufferIsWrapped_ShouldReturnWrappedItemsInOrder()
    {
        var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        buffer.Enqueue(4); // overwrites 1

        var view = new CircularBufferDebugView<int>(buffer);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, view.Items);
    }

    /// <summary>
    /// Verifies that DebugView retains a reference to the buffer even after external reference is cleared.
    /// </summary>
    [TestMethod]
    public void Items_WhenOriginalBufferIsCleared_ShouldStillExposeBufferContents()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        var view = new CircularBufferDebugView<int>(buffer);

        buffer = null; // Simulate external loss of reference

        CollectionAssert.AreEqual(new[] { 1, 2 }, view.Items);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Enqueue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue"/> replaces the single element when the buffer
    /// has capacity 1 and overwrite is enabled.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenBufferHasSingleSlot_ShouldReplaceExistingOnOverwrite()
    {
        var buffer = new CircularBuffer<string>(1, allowOverwrite: true);
        buffer.Enqueue("A");
        buffer.Enqueue("B"); // should evict "A"

        CollectionAssert.AreEqual(new[] { "B" }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue"/> overwrites the oldest item when the buffer is
    /// full and overwrite is allowed.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenFullAndOverwriteAllowed_ShouldOverwriteOldest()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        CollectionAssert.AreEqual(new[] { 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue"/> throws an exception when the buffer is full and
    /// overwrite is not allowed.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenFullAndOverwriteNotAllowed_ShouldThrowExactly()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue"/> retains <see langword="null"/> values in order.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenMultipleNullsProvided_ShouldRetainInOrder()
    {
        var buffer = new CircularBuffer<string?>(3);
        buffer.Enqueue(null);
        buffer.Enqueue("X");
        buffer.Enqueue(null);

        CollectionAssert.AreEqual(new[] { null, "X", null }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue"/> accepts <see langword="null"/> for reference types.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenNullValueProvided_ShouldAcceptNullReference()
    {
        var buffer = new CircularBuffer<string?>(2);
        buffer.Enqueue(null);

        Assert.AreEqual(1, buffer.Count);
        Assert.IsNull(buffer.Peek());
    }

    /// <summary>
    /// Verifies that head and tail wrap correctly when the buffer is full and overwrite is enabled.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenWraparoundOccursWithOverwrite_ShouldMaintainOrder()
    {
        var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        buffer.Dequeue();
        buffer.Enqueue(4); // wraps around

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, buffer.ToArray());
    }

}

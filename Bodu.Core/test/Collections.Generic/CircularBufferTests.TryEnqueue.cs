// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.TryEnqueue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.TryEnqueue"/> returns <see langword="false"/> when the
    /// buffer is full and overwriting is disabled.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenFullAndOverwriteDisabled_ShouldReturnFalse()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);

        var actual = buffer.TryEnqueue(3);

        Assert.IsFalse(actual);
        Assert.AreEqual(2, buffer.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.TryEnqueue"/> overwrites the oldest item and returns
    /// <see langword="true"/> when full and overwrite is enabled.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenFullAndOverwriteEnabled_ShouldOverwriteAndReturnTrue()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);

        var actual = buffer.TryEnqueue(3);

        Assert.IsTrue(actual);
        Assert.AreEqual(2, buffer.Count);
        CollectionAssert.AreEqual(new[] { 2, 3 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.TryEnqueue"/> triggers <see cref="CircularBuffer{T}.ItemEvicting"/>
    /// and <see cref="CircularBuffer{T}.ItemEvicted"/> in the correct order when overwriting.
    /// </summary>
    [TestMethod]
    public void TryEnqueue_WhenFullAndOverwriteEnabled_ShouldTriggerEvictionEventsInCorrectOrder()
    {
        var events = new List<string>();
        var buffer = new CircularBuffer<string>(2, allowOverwrite: true);

        buffer.TryEnqueue("A");
        buffer.TryEnqueue("B");

        buffer.ItemEvicting += item => events.Add("Evicting:" + item);
        buffer.ItemEvicted += item => events.Add("Evicted:" + item);

        var actual = buffer.TryEnqueue("C"); // should evict "A"

        Assert.IsTrue(actual);
        CollectionAssert.AreEqual(new[] { "Evicting:A", "Evicted:A" }, events);
        CollectionAssert.AreEqual(new[] { "B", "C" }, buffer.ToArray());
    }

}

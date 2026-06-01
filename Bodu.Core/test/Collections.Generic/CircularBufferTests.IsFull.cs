// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.IsFull.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.IsEmpty"/> is restored to <see langword="true"/> after
    /// <see cref="RingBackedCollection{T}.Clear"/>, and <see cref="RingBackedCollection{T}.IsFull"/> becomes
    /// <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_AfterClear_ShouldBeFalseAndIsEmptyShouldBeTrue()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        buffer.Clear();

        Assert.IsTrue(buffer.IsEmpty);
        Assert.IsFalse(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.IsFull"/> reverts to <see langword="false"/> after a removal
    /// from a full buffer.
    /// </summary>
    [TestMethod]
    public void IsFull_AfterDequeueFromFull_ShouldBeFalse()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        Assert.IsTrue(buffer.IsFull);

        _ = buffer.Dequeue();
        Assert.IsFalse(buffer.IsFull);
        Assert.IsFalse(buffer.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.IsFull"/> becomes <see langword="true"/> when
    /// <see cref="RingBackedCollection{T}.Count"/> reaches <see cref="RingBackedCollection{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenAtCapacity_ShouldBecomeTrue()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
        buffer.Enqueue(1);
        Assert.IsFalse(buffer.IsFull);

        buffer.Enqueue(2);
        Assert.IsTrue(buffer.IsFull);
        Assert.IsFalse(buffer.IsEmpty);
    }
    /// <summary>
    /// Verifies that a freshly constructed buffer reports <see cref="RingBackedCollection{T}.IsEmpty"/> as
    /// <see langword="true"/> and <see cref="RingBackedCollection{T}.IsFull"/> as <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenNewlyConstructed_ShouldBeFalseAndIsEmptyShouldBeTrue()
    {
        var buffer = new CircularBuffer<int>(3);

        Assert.IsTrue(buffer.IsEmpty);
        Assert.IsFalse(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="RingBackedCollection{T}.IsFull"/> remains <see langword="true"/> after an overwrite,
    /// because eviction keeps <see cref="RingBackedCollection{T}.Count"/> equal to
    /// <see cref="RingBackedCollection{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenOverwriteOccurs_ShouldRemainTrue()
    {
        var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        Assert.IsTrue(buffer.IsFull);

        buffer.Enqueue(3); // evicts 1
        Assert.IsTrue(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that a partially filled buffer reports both <see cref="RingBackedCollection{T}.IsEmpty"/> and
    /// <see cref="RingBackedCollection{T}.IsFull"/> as <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenPartiallyFilled_ShouldBeFalseAndIsEmptyShouldBeFalse()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);

        Assert.IsFalse(buffer.IsEmpty);
        Assert.IsFalse(buffer.IsFull);
    }

}

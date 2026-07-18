// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.Clear" /> removes all elements and resets the count to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferHasElements_ShouldRemoveAllElements()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 10; i++)
            buffer.Add(i);

        buffer.Clear();

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(0, buffer.ToArray().Length);
    }

    /// <summary>
    /// Verifies that a buffer can be reused after <see cref="SegmentedBuffer{T}.Clear" /> and continues appending from
    /// index zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferReusedAfterClear_ShouldAppendFromZero()
    {
        var buffer = new SegmentedBuffer<int>(3);
        for (int i = 0; i < 7; i++)
            buffer.Add(i);

        buffer.Clear();
        buffer.Add(99);
        buffer.Add(100);

        Assert.AreEqual(2, buffer.Count);
        Assert.AreEqual(99, buffer[0]);
        Assert.AreEqual(100, buffer[1]);
    }

    /// <summary>
    /// Verifies that clearing a buffer during enumeration causes the next iteration step to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 6; i++)
            buffer.Add(i);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int value in buffer)
                buffer.Clear();
        });
    }

    /// <summary>
    /// Verifies that clearing an already-empty buffer is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBufferIsEmpty_ShouldNotInvalidateEnumerator()
    {
        var buffer = new SegmentedBuffer<int>(4);

        buffer.Clear();

        Assert.AreEqual(0, buffer.Count);
    }
}

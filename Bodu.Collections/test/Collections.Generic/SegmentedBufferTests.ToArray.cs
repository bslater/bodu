// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.ToArray" /> returns the elements in insertion order when the buffer
    /// spans multiple segments.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenBufferSpansSegments_ShouldReturnInsertionOrderedElements()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 11; i++)
            buffer.Add(i * 2);

        int[] actual = buffer.ToArray();

        CollectionAssert.AreEqual(Enumerable.Range(0, 11).Select(i => i * 2).ToArray(), actual);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.ToArray" /> returns an empty array for an empty buffer.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenBufferIsEmpty_ShouldReturnEmptyArray()
    {
        var buffer = new SegmentedBuffer<int>();

        int[] actual = buffer.ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.ToArray" /> returns a fresh array that does not alias the buffer's
    /// internal storage.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenMutatedAfterCall_ShouldNotAffectBuffer()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 5; i++)
            buffer.Add(i);

        int[] snapshot = buffer.ToArray();
        snapshot[0] = 999;

        Assert.AreEqual(0, buffer[0]);
    }
}

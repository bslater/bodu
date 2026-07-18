// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.CopyTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.CopyTo" /> copies all elements in insertion order, spanning
    /// multiple segments.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenBufferSpansSegments_ShouldCopyInsertionOrderedElements()
    {
        var buffer = new SegmentedBuffer<int>(3);
        for (int i = 0; i < 10; i++)
            buffer.Add(i);

        var destination = new int[10];
        buffer.CopyTo(destination, 0);

        CollectionAssert.AreEqual(Enumerable.Range(0, 10).ToArray(), destination);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.CopyTo" /> honors a non-zero destination offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationOffsetIsNonZero_ShouldCopyStartingAtOffset()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 5; i++)
            buffer.Add(i);

        var destination = new int[8];
        buffer.CopyTo(destination, 2);

        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 2, 3, 4, 0 }, destination);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.CopyTo" /> throws <see cref="ArgumentNullException" /> when the
    /// destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            buffer.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.CopyTo" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// destination index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIndexIsNegative_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            buffer.CopyTo(new int[4], -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.CopyTo" /> throws <see cref="ArgumentException" /> when the
    /// destination is too small to hold the buffered elements.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 5; i++)
            buffer.Add(i);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            buffer.CopyTo(new int[4], 0);
        });
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.TrimExcess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.TrimExcess" /> preserves the buffered elements and their order.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenBufferHasElements_ShouldPreserveContents()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 13; i++)
            buffer.Add(i);

        buffer.TrimExcess();

        Assert.AreEqual(13, buffer.Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, 13).ToArray(), buffer.ToArray());
    }

    /// <summary>
    /// Verifies that the buffer continues to append correctly after <see cref="SegmentedBuffer{T}.TrimExcess" />.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenFollowedByAdd_ShouldContinueAppending()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 9; i++)
            buffer.Add(i);

        buffer.TrimExcess();
        buffer.Add(100);

        Assert.AreEqual(10, buffer.Count);
        Assert.AreEqual(100, buffer[9]);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 100 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.TrimExcess" /> invalidates an active enumerator.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalledDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 6; i++)
            buffer.Add(i);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int value in buffer)
                buffer.TrimExcess();
        });
    }
}

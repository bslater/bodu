// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBufferTests.AddSpan.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure;

public partial class ByteBufferTests
{
    /// <summary>
    /// Verifies that sliced spans are correctly added to the buffer.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingSpanSlice_ShouldAddCorrectBytes()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var buffer = new ByteBuffer(2);
        buffer.Add(data.AsSpan(1, 2)); // adds 2 and 3
        var result = buffer.GetBytesZeroPadded();
        CollectionAssert.AreEqual(new byte[] { 2, 3 }, result);
    }

    /// <summary>
    /// Verifies that adding a span that exceeds capacity throws ArgumentOutOfRangeException.
    /// </summary>
    [TestMethod]
    public void Add_WhenSpanExceedsRemainingCapacity_ShouldThrowExactly()
    {
        var buffer = new ByteBuffer(2);
        buffer.Add(new byte[] { 1 }, 0, 1);
        var span = new byte[] { 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Add(span));
    }

    /// <summary>
    /// Verifies that a valid span is added successfully.
    /// </summary>
    [TestMethod]
    public void Add_WhenValidSpanAdded_ShouldReturnCorrectly()
    {
        var buffer = new ByteBuffer(3);
        var span = new byte[] { 1, 2 };
        var result = buffer.Add(span);
        Assert.IsFalse(result);
        Assert.AreEqual(2, buffer.Count);
    }
}

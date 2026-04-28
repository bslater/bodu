// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBufferTests.IsFull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure;

public partial class ByteBufferTests
{
    /// <summary>
    /// Verifies that a buffer is full when filled to capacity.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenBufferFilledExactly_ShouldReturnTrue()
    {
        var buffer = new ByteBuffer(2);
        buffer.Add(new byte[] { 1, 2 }, 0, 2);
        Assert.IsTrue(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that a buffer not filled to capacity is not full.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenBufferHasRoomRemaining_ShouldReturnFalse()
    {
        var buffer = new ByteBuffer(3);
        buffer.Add(new byte[] { 1 }, 0, 1);
        Assert.IsFalse(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that a new buffer is not full.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenInitialized_ShouldReturnFalse()
    {
        var buffer = new ByteBuffer(2);
        Assert.IsFalse(buffer.IsFull);
    }

    /// <summary>
    /// Verifies that resetting a full buffer sets IsFull to false.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenResetAfterFull_ShouldReturnFalse()
    {
        var buffer = new ByteBuffer(1);
        buffer.Add(new byte[] { 1 }, 0, 1);
        buffer.Initialize();
        Assert.IsFalse(buffer.IsFull);
    }
}

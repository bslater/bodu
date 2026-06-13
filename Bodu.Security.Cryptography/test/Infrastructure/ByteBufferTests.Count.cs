// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBufferTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure;

public partial class ByteBufferTests
{
    /// <summary>
    /// Verifies that the count reflects bytes added.
    /// </summary>
    [TestMethod]
    public void Count_WhenBytesAdded_ShouldReturnCorrectCount()
    {
        var buffer = new ByteBuffer(4);
        buffer.Add([1, 2], 0, 2);
        Assert.AreEqual(2, buffer.Count);
    }

    /// <summary>
    /// Verifies that a new buffer has a count of 0.
    /// </summary>
    [TestMethod]
    public void Count_WhenInitialized_ShouldReturnZero()
    {
        var buffer = new ByteBuffer(4);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that resetting the buffer sets count to 0.
    /// </summary>
    [TestMethod]
    public void Count_WhenReset_ShouldReturnZero()
    {
        var buffer = new ByteBuffer(4);
        buffer.Add([1, 2], 0, 2);
        buffer.Initialize();
        Assert.AreEqual(0, buffer.Count);
    }
}

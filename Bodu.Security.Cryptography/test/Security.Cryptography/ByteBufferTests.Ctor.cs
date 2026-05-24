// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBufferTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure;

public partial class ByteBufferTests
{
    /// <summary>
    /// Verifies that the constructor throws when called with negative capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCalledWithNegativeCapacity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ByteBuffer(-1));
    }

    /// <summary>
    /// Verifies that the constructor sets the correct capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCalledWithPositiveCapacity_ShouldSetCapacity()
    {
        var buffer = new ByteBuffer(10);
        Assert.AreEqual(10, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that a buffer with zero capacity is immediately full.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZero_ShouldBeImmediatelyFull()
    {
        var buffer = new ByteBuffer(0);
        Assert.IsTrue(buffer.IsFull);
        Assert.IsTrue(buffer.IsEmpty); // also logically empty
    }

    /// <summary>
    /// Verifies that the buffer is initially empty after construction.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldBeEmpty()
    {
        var buffer = new ByteBuffer(5);
        Assert.IsTrue(buffer.IsEmpty);
    }
}

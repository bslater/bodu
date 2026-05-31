// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBufferTests.Capacity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure;

public partial class ByteBufferTests
{
    /// <summary>
    /// Verifies that the buffer's Capacity property returns the correct size.
    /// </summary>
    [TestMethod]
    public void Capacity_ShouldReturnSpecifiedCapacity()
    {
        var buffer = new ByteBuffer(8);
        Assert.AreEqual(8, buffer.Capacity);
    }
}

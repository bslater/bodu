// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.GetHashCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.GetHashCode" /> returns the same value for repeated calls on the same instance.
    /// </summary>
    [TestMethod]
    public void GetHashCode_ShouldBeStable_ForSameInstance()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        var hash1 = buffer.GetHashCode();
        var hash2 = buffer.GetHashCode();

        Assert.AreEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that GetHashCode returns different values for different instances (not
    /// guaranteed, but likely).
    /// </summary>
    [TestMethod]
    public void GetHashCode_ShouldBeDifferent_ForDistinctInstances()
    {
        var buffer1 = new CircularBuffer<int>(2);
        var buffer2 = new CircularBuffer<int>(2);

        buffer1.Enqueue(1);
        buffer2.Enqueue(1);

        // It's possible, but very unlikely, for these to match.
        Assert.AreNotEqual(buffer1.GetHashCode(), buffer2.GetHashCode(), "Hash codes matched but should ideally differ for different instances.");
    }
}

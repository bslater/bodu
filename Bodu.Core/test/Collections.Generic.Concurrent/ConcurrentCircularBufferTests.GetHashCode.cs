// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.GetHashCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that GetHashCode returns a stable value when called multiple times on the same instance.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenCalledMultipleTimes_ShouldReturnStableValue()
    {
        var buffer = new ConcurrentCircularBuffer<int>(2);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        int hash1 = buffer.GetHashCode();
        int hash2 = buffer.GetHashCode();

        Assert.AreEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that GetHashCode returns different values for two distinct instances, as expected
    /// from reference-based hash code semantics.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenCalledOnDistinctInstances_ShouldLikelyDiffer()
    {
        var buffer1 = new ConcurrentCircularBuffer<int>(2);
        var buffer2 = new ConcurrentCircularBuffer<int>(2);

        buffer1.Enqueue(1);
        buffer2.Enqueue(1);

        Assert.AreNotEqual(buffer1.GetHashCode(), buffer2.GetHashCode(),
            "Hash codes matched but should ideally differ for different instances.");
    }
}

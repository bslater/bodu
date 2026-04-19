// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that Equals returns <see langword="true"/> when comparing the buffer instance with itself.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingSameReference_ShouldReturnTrue()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);

        Assert.IsTrue(buffer.Equals(buffer));
    }

    /// <summary>
    /// Verifies that Equals returns <see langword="false"/> when comparing the buffer with <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingWithNull_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(1);

        Assert.IsFalse(buffer.Equals(null));
    }

    /// <summary>
    /// Verifies that Equals returns <see langword="false"/> when comparing the buffer with an object of a different type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDifferentType_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<string>(2);
        buffer.Enqueue("A");

        Assert.IsFalse(buffer.Equals("not a buffer"));
    }

    /// <summary>
    /// Verifies that Equals returns <see langword="false"/> when comparing two distinct buffer instances,
    /// even if they contain the same elements, because default reference equality is used.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingTwoDistinctInstancesWithSameContents_ShouldReturnFalse()
    {
        var buffer1 = new ConcurrentCircularBuffer<string>(3);
        var buffer2 = new ConcurrentCircularBuffer<string>(3);

        buffer1.Enqueue("A");
        buffer2.Enqueue("A");

        Assert.IsFalse(buffer1.Equals(buffer2));
    }
}

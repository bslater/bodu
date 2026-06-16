// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.EnsureCapacity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}.EnsureCapacity(int)"/> throws when given a negative argument.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowExactly()
    {
        var deque = new Deque<int>();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            deque.EnsureCapacity(-1);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Deque{T}.EnsureCapacity(int)"/> grows the deque to the requested minimum.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityIsLarger_ShouldGrow()
    {
        var deque = new Deque<int>(4);
        int newCap = deque.EnsureCapacity(50);
        Assert.IsGreaterThanOrEqualTo(50, newCap);
        Assert.IsGreaterThanOrEqualTo(50, deque.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.EnsureCapacity(int)"/> is a no-op when the deque already meets the requested size.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityIsSmaller_ShouldNotShrink()
    {
        var deque = new Deque<int>(50);
        int original = deque.Capacity;
        int newCap = deque.EnsureCapacity(10);
        Assert.AreEqual(original, newCap);
        Assert.AreEqual(original, deque.Capacity);
    }

}

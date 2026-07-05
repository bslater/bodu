// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Length.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Length" /> is zero for an empty set regardless of the allocated capacity.
    /// </summary>
    [TestMethod]
    public void Length_WhenSetIsEmpty_ShouldBeZeroRegardlessOfCapacity()
    {
        var bits = new BitSet(1024);

        Assert.AreEqual(0, bits.Length);
        Assert.AreEqual(1024, bits.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Length" /> is one greater than the highest set-bit index.
    /// </summary>
    [TestMethod]
    public void Length_WhenBitsSet_ShouldBeHighestSetBitPlusOne()
    {
        var bits = CreateWithBits(0, 5, 64);

        Assert.AreEqual(65, bits.Length);
    }

    /// <summary>
    /// Verifies that clearing the highest set bit reduces <see cref="BitSet.Length" /> while
    /// <see cref="BitSet.Capacity" /> is unchanged.
    /// </summary>
    [TestMethod]
    public void Length_WhenHighestBitCleared_ShouldShrinkWhileCapacityIsRetained()
    {
        var bits = CreateWithBits(5, 200);
        var capacity = bits.Capacity;

        bits.Clear(200);

        Assert.AreEqual(6, bits.Length);
        Assert.AreEqual(capacity, bits.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Length" /> and <see cref="BitSet.Capacity" /> diverge after auto-growth: the
    /// length tracks the logical content while the capacity reflects the allocation.
    /// </summary>
    [TestMethod]
    public void Length_WhenStorageGrows_ShouldTrackContentNotAllocation()
    {
        var bits = new BitSet(64);

        bits.Set(129);

        Assert.AreEqual(130, bits.Length);
        Assert.IsTrue(bits.Capacity >= 130);
        Assert.AreEqual(0, bits.Capacity % 64);
    }
}

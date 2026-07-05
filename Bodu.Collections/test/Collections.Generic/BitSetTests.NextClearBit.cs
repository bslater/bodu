// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.NextClearBit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.NextClearBit" /> returns the index itself when the bit at the start index is
    /// clear.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenBitAtFromIndexIsClear_ShouldReturnFromIndex()
    {
        var bits = CreateWithBits(10);

        Assert.AreEqual(0, bits.NextClearBit(0));
        Assert.AreEqual(11, bits.NextClearBit(11));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextClearBit" /> skips a run of set bits to the first clear bit.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenBitsAtFromIndexAreSet_ShouldSkipToFirstClearBit()
    {
        var bits = new BitSet();
        bits.Set(0, 10);

        Assert.AreEqual(10, bits.NextClearBit(0));
        Assert.AreEqual(10, bits.NextClearBit(5));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextClearBit" /> crosses word boundaries when an entire word is set.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenWholeWordIsSet_ShouldCrossWordBoundary()
    {
        var bits = new BitSet();
        bits.Set(0, 64);

        Assert.AreEqual(64, bits.NextClearBit(0));
        Assert.AreEqual(64, bits.NextClearBit(63));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextClearBit" /> returns the capacity when every allocated bit from the start
    /// index onward is set — the first conceptually clear bit lies beyond the storage.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenAllAllocatedBitsSet_ShouldReturnCapacity()
    {
        var bits = new BitSet(64);
        bits.Set(0, 64);

        Assert.AreEqual(64, bits.Capacity);
        Assert.AreEqual(64, bits.NextClearBit(0));
    }

    /// <summary>
    /// Verifies that a start index at or beyond the capacity is returned unchanged — such bits are conceptually
    /// clear, so the result is never −1.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenFromIndexBeyondCapacity_ShouldReturnFromIndex()
    {
        var bits = CreateWithBits(5);

        Assert.AreEqual(bits.Capacity, bits.NextClearBit(bits.Capacity));
        Assert.AreEqual(int.MaxValue, bits.NextClearBit(int.MaxValue));
    }

    /// <summary>
    /// Verifies that a negative start index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextClearBit_WhenFromIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = bits.NextClearBit(-1);
        });

        Assert.AreEqual("fromIndex", ex.ParamName);
    }
}

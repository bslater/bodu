// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Flip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Flip(int)" /> toggles a bit in both directions.
    /// </summary>
    [TestMethod]
    public void Flip_WhenCalledTwice_ShouldToggleBitBothWays()
    {
        var bits = new BitSet();

        bits.Flip(12);
        Assert.IsTrue(bits.Get(12));

        bits.Flip(12);
        Assert.IsFalse(bits.Get(12));
    }

    /// <summary>
    /// Verifies that flipping a clear bit beyond the capacity sets it and grows the storage.
    /// </summary>
    [TestMethod]
    public void Flip_WhenIndexBeyondCapacity_ShouldAutoGrowAndSetBit()
    {
        var bits = new BitSet(64);

        bits.Flip(300);

        Assert.IsTrue(bits.Get(300));
        Assert.IsTrue(bits.Capacity >= 301);
        Assert.AreEqual(301, bits.Length);
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Flip_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Flip(-1);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an index at or beyond <see cref="BitSet.MaxBitCount" /> throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Flip_WhenIndexExceedsMaxBitCount_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Flip(BitSet.MaxBitCount);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the range overload toggles exactly the bits in the half-open range.
    /// </summary>
    [TestMethod]
    public void Flip_WhenRangeProvided_ShouldToggleHalfOpenRange()
    {
        var bits = CreateWithBits(2, 4);

        bits.Flip(1, 5);

        CollectionAssert.AreEqual(new[] { 1, 3 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that flipping a range beyond the capacity grows the storage and sets the previously clear bits.
    /// </summary>
    [TestMethod]
    public void Flip_WhenRangeBeyondCapacity_ShouldAutoGrow()
    {
        var bits = new BitSet(0);

        bits.Flip(100, 104);

        CollectionAssert.AreEqual(new[] { 100, 101, 102, 103 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that an empty range is a no-op.
    /// </summary>
    [TestMethod]
    public void Flip_WhenRangeIsEmpty_ShouldBeNoOp()
    {
        var bits = CreateWithBits(7);

        bits.Flip(7, 7);

        Assert.IsTrue(bits.Get(7));
        Assert.AreEqual(1, bits.Cardinality);
    }
}

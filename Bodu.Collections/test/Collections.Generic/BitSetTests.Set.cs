// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Set.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Set(int)" /> sets the bit at the specified index.
    /// </summary>
    [TestMethod]
    public void Set_WhenIndexWithinCapacity_ShouldSetBit()
    {
        var bits = new BitSet(64);

        bits.Set(7);

        Assert.IsTrue(bits.Get(7));
        Assert.AreEqual(1, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that setting a bit beyond the current capacity grows the storage automatically.
    /// </summary>
    [TestMethod]
    public void Set_WhenIndexBeyondCapacity_ShouldAutoGrow()
    {
        var bits = new BitSet(64);

        bits.Set(1000);

        Assert.IsTrue(bits.Get(1000));
        Assert.IsTrue(bits.Capacity >= 1001);
        Assert.AreEqual(1001, bits.Length);
    }

    /// <summary>
    /// Verifies that setting a bit on a zero-capacity set grows from empty storage.
    /// </summary>
    [TestMethod]
    public void Set_WhenCapacityIsZero_ShouldAutoGrowFromEmptyStorage()
    {
        var bits = new BitSet(0);

        bits.Set(0);

        Assert.IsTrue(bits.Get(0));
        Assert.AreEqual(1, bits.Length);
    }

    /// <summary>
    /// Verifies that growth preserves previously set bits.
    /// </summary>
    [TestMethod]
    public void Set_WhenGrowthOccurs_ShouldPreserveExistingBits()
    {
        var bits = CreateWithBits(0, 33, 63);

        bits.Set(5000);

        Assert.IsTrue(bits.Get(0));
        Assert.IsTrue(bits.Get(33));
        Assert.IsTrue(bits.Get(63));
        Assert.IsTrue(bits.Get(5000));
        Assert.AreEqual(4, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that setting an already-set bit is idempotent.
    /// </summary>
    [TestMethod]
    public void Set_WhenBitAlreadySet_ShouldRemainSet()
    {
        var bits = CreateWithBits(9);

        bits.Set(9);

        Assert.IsTrue(bits.Get(9));
        Assert.AreEqual(1, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that the value overload sets the bit when the value is <see langword="true" /> and clears it when the
    /// value is <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueProvided_ForBothValues_ShouldWriteBit()
    {
        var bits = new BitSet();

        bits.Set(11, true);
        Assert.IsTrue(bits.Get(11));

        bits.Set(11, false);
        Assert.IsFalse(bits.Get(11));
    }

    /// <summary>
    /// Verifies that writing <see langword="false" /> beyond the capacity is a no-op that does not grow the storage.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueIsFalseBeyondCapacity_ShouldNotGrowStorage()
    {
        var bits = new BitSet(64);

        bits.Set(10_000, false);

        Assert.AreEqual(64, bits.Capacity);
        Assert.IsFalse(bits.Get(10_000));
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Set(-1);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an index at or beyond <see cref="BitSet.MaxBitCount" /> throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenIndexExceedsMaxBitCount_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Set(BitSet.MaxBitCount);
        });

        Assert.AreEqual("index", ex.ParamName);
    }
}

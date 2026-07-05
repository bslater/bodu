// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Get.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Get(int)" /> returns <see langword="true" /> for set bits and
    /// <see langword="false" /> for clear bits within the capacity.
    /// </summary>
    [TestMethod]
    public void Get_WhenBitWithinCapacity_ShouldReturnBitValue()
    {
        var bits = CreateWithBits(0, 5, 63);

        Assert.IsTrue(bits.Get(0));
        Assert.IsTrue(bits.Get(5));
        Assert.IsTrue(bits.Get(63));
        Assert.IsFalse(bits.Get(1));
        Assert.IsFalse(bits.Get(62));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Get(int)" /> returns <see langword="false" /> for indices at or beyond the
    /// capacity instead of throwing.
    /// </summary>
    [TestMethod]
    public void Get_WhenIndexBeyondCapacity_ShouldReturnFalseWithoutThrowing()
    {
        var bits = new BitSet(64);

        Assert.IsFalse(bits.Get(64));
        Assert.IsFalse(bits.Get(1_000_000));
        Assert.IsFalse(bits.Get(int.MaxValue));
        Assert.AreEqual(64, bits.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Get(int)" /> never grows the storage, even for indices far beyond the capacity.
    /// </summary>
    [TestMethod]
    public void Get_WhenIndexBeyondCapacity_ShouldNotGrowStorage()
    {
        var bits = new BitSet(0);

        _ = bits.Get(1_000_000);

        Assert.AreEqual(0, bits.Capacity);
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Get_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = bits.Get(-1);
        });

        Assert.AreEqual("index", ex.ParamName);
    }
}

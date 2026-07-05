// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.NextSetBit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.NextSetBit" /> returns the index itself when the bit at the start index is
    /// set.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenBitAtFromIndexIsSet_ShouldReturnFromIndex()
    {
        var bits = CreateWithBits(10);

        Assert.AreEqual(10, bits.NextSetBit(10));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextSetBit" /> walks the set bits in ascending order via the
    /// <c>NextSetBit(i + 1)</c> idiom.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenWalkingFromZero_ShouldVisitSetBitsAscending()
    {
        var bits = CreateWithBits(3, 64, 65, 191);

        var visited = new List<int>();
        for (var i = bits.NextSetBit(0); i >= 0; i = bits.NextSetBit(i + 1))
            visited.Add(i);

        CollectionAssert.AreEqual(new[] { 3, 64, 65, 191 }, visited);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextSetBit" /> crosses word boundaries — a search starting in one word finds
    /// the first set bit in a later word.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenNextBitIsInLaterWord_ShouldCrossWordBoundary()
    {
        var bits = CreateWithBits(0, 130);

        Assert.AreEqual(130, bits.NextSetBit(1));
        Assert.AreEqual(130, bits.NextSetBit(64));
        Assert.AreEqual(130, bits.NextSetBit(130));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.NextSetBit" /> returns −1 when no set bit exists at or after the start index.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenNoFurtherSetBit_ShouldReturnMinusOne()
    {
        var bits = CreateWithBits(5);

        Assert.AreEqual(-1, bits.NextSetBit(6));
        Assert.AreEqual(-1, new BitSet().NextSetBit(0));
    }

    /// <summary>
    /// Verifies that a start index at or beyond the capacity returns −1 without throwing.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenFromIndexBeyondCapacity_ShouldReturnMinusOne()
    {
        var bits = CreateWithBits(5);

        Assert.AreEqual(-1, bits.NextSetBit(bits.Capacity));
        Assert.AreEqual(-1, bits.NextSetBit(int.MaxValue));
    }

    /// <summary>
    /// Verifies that a negative start index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextSetBit_WhenFromIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = bits.NextSetBit(-1);
        });

        Assert.AreEqual("fromIndex", ex.ParamName);
    }
}

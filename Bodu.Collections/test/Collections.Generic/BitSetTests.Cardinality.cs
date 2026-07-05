// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Cardinality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Cardinality" /> is zero for an empty set and <see cref="BitSet.IsEmpty" /> is
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Cardinality_WhenSetIsEmpty_ShouldBeZero()
    {
        var bits = new BitSet();

        Assert.AreEqual(0, bits.Cardinality);
        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Cardinality" /> counts set bits across multiple words.
    /// </summary>
    [TestMethod]
    public void Cardinality_WhenBitsSpanMultipleWords_ShouldCountAllSetBits()
    {
        var bits = CreateWithBits(0, 63, 64, 127, 128, 500);

        Assert.AreEqual(6, bits.Cardinality);
        Assert.IsFalse(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Cardinality" /> tracks mutations — set, clear, flip, and range operations.
    /// </summary>
    [TestMethod]
    public void Cardinality_WhenBitsMutated_ShouldTrackChanges()
    {
        var bits = new BitSet();

        bits.Set(0, 100);
        Assert.AreEqual(100, bits.Cardinality);

        bits.Clear(0, 50);
        Assert.AreEqual(50, bits.Cardinality);

        bits.Flip(0, 100);
        Assert.AreEqual(50, bits.Cardinality);

        bits.Clear();
        Assert.AreEqual(0, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that a fully set range reports a cardinality equal to the range width.
    /// </summary>
    [TestMethod]
    public void Cardinality_WhenRangeFullySet_ShouldEqualRangeWidth()
    {
        var bits = new BitSet();

        bits.Set(0, 1000);

        Assert.AreEqual(1000, bits.Cardinality);
    }
}

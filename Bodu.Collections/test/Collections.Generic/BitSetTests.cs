// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class BitSetTests
{
    /// <summary>
    /// Creates a <see cref="BitSet" /> with the specified bit indices set.
    /// </summary>
    /// <param name="indices">The bit indices to set.</param>
    /// <returns>A new <see cref="BitSet" /> whose set bits are exactly <paramref name="indices" />.</returns>
    private static BitSet CreateWithBits(params int[] indices)
    {
        var bits = new BitSet();
        foreach (var index in indices)
            bits.Set(index);

        return bits;
    }

    /// <summary>
    /// Verifies that bits can be set — including beyond the initial capacity — queried, and enumerated in ascending
    /// index order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void BitSet_WhenBitsSet_ShouldQueryAndEnumerate()
    {
        var bits = new BitSet();
        bits.Set(1);
        bits.Set(64);
        bits.Set(200);

        Assert.AreEqual(3, bits.Cardinality);
        Assert.AreEqual(201, bits.Length);
        Assert.IsTrue(bits.Get(64));
        Assert.IsFalse(bits.Get(63));
        CollectionAssert.AreEqual(new[] { 1, 64, 200 }, bits.ToArray());
    }
}

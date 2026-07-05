// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that the indexer getter mirrors <see cref="BitSet.Get(int)" />, including the beyond-capacity
    /// <see langword="false" /> contract.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenReading_ShouldMirrorGet()
    {
        var bits = CreateWithBits(2);

        Assert.IsTrue(bits[2]);
        Assert.IsFalse(bits[3]);
        Assert.IsFalse(bits[1_000_000]);
    }

    /// <summary>
    /// Verifies that the indexer setter mirrors <see cref="BitSet.Set(int, bool)" />, including auto-growth.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenWriting_ShouldMirrorSet()
    {
        var bits = new BitSet(64);

        bits[500] = true;
        Assert.IsTrue(bits.Get(500));
        Assert.IsTrue(bits.Capacity >= 501);

        bits[500] = false;
        Assert.IsFalse(bits.Get(500));
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" /> through both accessors.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var getEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = bits[-1];
        });
        var setEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits[-1] = true;
        });

        Assert.AreEqual("index", getEx.ParamName);
        Assert.AreEqual("index", setEx.ParamName);
    }
}

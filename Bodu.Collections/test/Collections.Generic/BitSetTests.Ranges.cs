// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Ranges.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Set(int, int)" /> sets exactly the bits in the half-open range, growing as
    /// required.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeProvided_ShouldSetHalfOpenRange()
    {
        var bits = new BitSet();

        bits.Set(3, 8);

        CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7 }, bits.ToArray());
        Assert.IsFalse(bits.Get(8));
    }

    /// <summary>
    /// Verifies that ranges straddling a 64-bit word boundary (63..65) affect both words correctly for set, clear,
    /// and flip.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeStraddlesWordBoundary_ShouldAffectBothWords()
    {
        var bits = new BitSet();

        bits.Set(63, 65);
        CollectionAssert.AreEqual(new[] { 63, 64 }, bits.ToArray());

        bits.Flip(62, 66);
        CollectionAssert.AreEqual(new[] { 62, 65 }, bits.ToArray());

        bits.Clear(60, 70);
        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that a range spanning several whole words fills the interior words completely.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeSpansMultipleWords_ShouldFillInteriorWords()
    {
        var bits = new BitSet();

        bits.Set(10, 200);

        Assert.AreEqual(190, bits.Cardinality);
        Assert.IsFalse(bits.Get(9));
        Assert.IsTrue(bits.Get(10));
        Assert.IsTrue(bits.Get(64));
        Assert.IsTrue(bits.Get(128));
        Assert.IsTrue(bits.Get(199));
        Assert.IsFalse(bits.Get(200));
    }

    /// <summary>
    /// Verifies that a range entirely within a single word only touches the masked bits.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeWithinSingleWord_ShouldOnlyTouchMaskedBits()
    {
        var bits = CreateWithBits(0, 63);

        bits.Set(30, 34);

        CollectionAssert.AreEqual(new[] { 0, 30, 31, 32, 33, 63 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that an empty set range is a no-op.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeIsEmpty_ShouldBeNoOp()
    {
        var bits = new BitSet();

        bits.Set(40, 40);

        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that a range whose start exceeds its end throws <see cref="ArgumentException" /> for set, clear, and
    /// flip.
    /// </summary>
    [TestMethod]
    public void Set_WhenFromExceedsTo_ShouldThrowArgumentException()
    {
        var bits = new BitSet();

        var setEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            bits.Set(5, 4);
        });
        var clearEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            bits.Clear(5, 4);
        });
        var flipEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            bits.Flip(5, 4);
        });

        Assert.AreEqual("fromInclusive", setEx.ParamName);
        Assert.AreEqual("fromInclusive", clearEx.ParamName);
        Assert.AreEqual("fromInclusive", flipEx.ParamName);
    }

    /// <summary>
    /// Verifies that a negative range start throws <see cref="ArgumentOutOfRangeException" /> for set, clear, and
    /// flip.
    /// </summary>
    [TestMethod]
    public void Set_WhenFromIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var setEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Set(-1, 4);
        });
        var clearEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Clear(-1, 4);
        });
        var flipEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Flip(-1, 4);
        });

        Assert.AreEqual("fromInclusive", setEx.ParamName);
        Assert.AreEqual("fromInclusive", clearEx.ParamName);
        Assert.AreEqual("fromInclusive", flipEx.ParamName);
    }

    /// <summary>
    /// Verifies that a range end above <see cref="BitSet.MaxBitCount" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for the growing range overloads.
    /// </summary>
    [TestMethod]
    public void Set_WhenToExceedsMaxBitCount_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var setEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Set(0, BitSet.MaxBitCount + 1);
        });
        var flipEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Flip(0, BitSet.MaxBitCount + 1);
        });

        Assert.AreEqual("toExclusive", setEx.ParamName);
        Assert.AreEqual("toExclusive", flipEx.ParamName);
    }

    /// <summary>
    /// Verifies that a word-aligned range end (a multiple of 64) does not spill into the following word.
    /// </summary>
    [TestMethod]
    public void Set_WhenRangeEndIsWordAligned_ShouldNotSpillIntoNextWord()
    {
        var bits = new BitSet();

        bits.Set(120, 128);

        Assert.AreEqual(8, bits.Cardinality);
        Assert.IsTrue(bits.Get(127));
        Assert.IsFalse(bits.Get(128));
        Assert.AreEqual(128, bits.Length);
    }
}

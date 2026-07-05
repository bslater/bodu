// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty set with a small non-zero default capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldCreateEmptySetWithDefaultCapacity()
    {
        var bits = new BitSet();

        Assert.IsTrue(bits.IsEmpty);
        Assert.AreEqual(0, bits.Length);
        Assert.AreEqual(64, bits.Capacity);
    }

    /// <summary>
    /// Verifies that a zero initial capacity is accepted and allocates no storage.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCapacityIsZero_ShouldCreateEmptySetWithZeroCapacity()
    {
        var bits = new BitSet(0);

        Assert.IsTrue(bits.IsEmpty);
        Assert.AreEqual(0, bits.Capacity);
        Assert.AreEqual(0, bits.Length);
    }

    /// <summary>
    /// Verifies that the requested capacity is rounded up to a whole number of 64-bit words.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCapacityIsNotWordAligned_ShouldRoundCapacityUpToWholeWords()
    {
        Assert.AreEqual(64, new BitSet(1).Capacity);
        Assert.AreEqual(64, new BitSet(64).Capacity);
        Assert.AreEqual(128, new BitSet(65).Capacity);
        Assert.AreEqual(192, new BitSet(130).Capacity);
    }

    /// <summary>
    /// Verifies that a negative initial capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCapacityIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BitSet(-1);
        });

        Assert.AreEqual("initialCapacityBits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an initial capacity above <see cref="BitSet.MaxBitCount" /> throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCapacityExceedsMaxBitCount_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BitSet(BitSet.MaxBitCount + 1);
        });

        Assert.AreEqual("initialCapacityBits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the copy constructor produces an equal set that shares no storage with the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCopyingSource_ShouldCopyBitsWithoutSharingStorage()
    {
        var source = CreateWithBits(3, 64, 190);

        var copy = new BitSet(source);

        Assert.AreEqual(source, copy);
        Assert.AreEqual(source.Capacity, copy.Capacity);

        copy.Set(500);
        Assert.IsFalse(source.Get(500));

        source.Clear(3);
        Assert.IsTrue(copy.Get(3));
    }

    /// <summary>
    /// Verifies that the copy constructor throws <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BitSet(null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}

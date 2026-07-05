// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.Clear(int)" /> clears a set bit.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBitIsSet_ShouldClearBit()
    {
        var bits = CreateWithBits(4, 5);

        bits.Clear(4);

        Assert.IsFalse(bits.Get(4));
        Assert.IsTrue(bits.Get(5));
        Assert.AreEqual(1, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that clearing an already-clear bit is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenBitAlreadyClear_ShouldRemainClear()
    {
        var bits = CreateWithBits(3);

        bits.Clear(10);

        Assert.IsTrue(bits.Get(3));
        Assert.AreEqual(1, bits.Cardinality);
    }

    /// <summary>
    /// Verifies that clearing a bit beyond the capacity is a no-op that neither throws nor grows the storage.
    /// </summary>
    [TestMethod]
    public void Clear_WhenIndexBeyondCapacity_ShouldBeNoOpWithoutGrowth()
    {
        var bits = new BitSet(64);

        bits.Clear(1_000_000);
        bits.Clear(int.MaxValue);

        Assert.AreEqual(64, bits.Capacity);
        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var bits = new BitSet();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            bits.Clear(-1);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="BitSet.Clear()" /> overload clears every bit while retaining the
    /// allocated capacity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAllBitsCleared_ShouldEmptySetAndRetainCapacity()
    {
        var bits = CreateWithBits(1, 64, 500);
        var capacity = bits.Capacity;

        bits.Clear();

        Assert.IsTrue(bits.IsEmpty);
        Assert.AreEqual(0, bits.Length);
        Assert.AreEqual(0, bits.Cardinality);
        Assert.AreEqual(capacity, bits.Capacity);
    }

    /// <summary>
    /// Verifies that the range overload clears exactly the bits in the half-open range.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRangeProvided_ShouldClearHalfOpenRange()
    {
        var bits = new BitSet();
        bits.Set(0, 10);

        bits.Clear(2, 5);

        CollectionAssert.AreEqual(new[] { 0, 1, 5, 6, 7, 8, 9 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that a range extending beyond the capacity is clamped and neither throws nor grows the storage.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRangeExtendsBeyondCapacity_ShouldClampWithoutGrowth()
    {
        var bits = CreateWithBits(10, 60);
        var capacity = bits.Capacity;

        bits.Clear(0, int.MaxValue);

        Assert.IsTrue(bits.IsEmpty);
        Assert.AreEqual(capacity, bits.Capacity);
    }

    /// <summary>
    /// Verifies that an empty range is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRangeIsEmpty_ShouldBeNoOp()
    {
        var bits = CreateWithBits(5);

        bits.Clear(5, 5);

        Assert.IsTrue(bits.Get(5));
    }
}

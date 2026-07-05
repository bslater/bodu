// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that two sets with the same set bits are equal and hash identically, regardless of how they were
    /// built.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameSetBits_ShouldBeEqualWithSameHashCode()
    {
        var left = CreateWithBits(1, 64, 200);
        var right = new BitSet(4096);
        right.Set(1);
        right.Set(64);
        right.Set(200);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that trailing zero words are ignored: setting a high bit and clearing it again leaves the set equal
    /// to a fresh empty set despite the grown capacity.
    /// </summary>
    [TestMethod]
    public void Equals_WhenHighBitSetThenCleared_ShouldEqualFreshEmptySet()
    {
        var grown = new BitSet();
        grown.Set(200);
        grown.Clear(200);
        var fresh = new BitSet();

        Assert.IsTrue(grown.Capacity > fresh.Capacity);
        Assert.IsTrue(grown.Equals(fresh));
        Assert.IsTrue(fresh.Equals(grown));
        Assert.AreEqual(fresh.GetHashCode(), grown.GetHashCode());
    }

    /// <summary>
    /// Verifies that sets differing in one bit are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBitsDiffer_ShouldNotBeEqual()
    {
        var left = CreateWithBits(1, 64);
        var right = CreateWithBits(1, 65);

        Assert.IsFalse(left.Equals(right));
        Assert.IsFalse(left.Equals(CreateWithBits(1)));
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Equals(object)" /> returns <see langword="false" /> for <see langword="null" />
    /// and for non-<see cref="BitSet" /> objects, and <see langword="true" /> for the same instance.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToNullOrOtherType_ShouldReturnFalse()
    {
        var bits = CreateWithBits(1);

        Assert.IsFalse(bits.Equals(null));
        Assert.IsFalse(bits.Equals("BitSet"));
        Assert.IsTrue(bits.Equals(bits));
    }

    /// <summary>
    /// Verifies that the equality operators mirror <see cref="BitSet.Equals(BitSet)" /> and handle
    /// <see langword="null" /> operands on either side.
    /// </summary>
    [TestMethod]
    public void OperatorEquality_WhenOperandsCompared_ShouldMatchEqualsIncludingNulls()
    {
        var left = CreateWithBits(1, 64);
        var right = CreateWithBits(1, 64);
        BitSet? nullSet = null;

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.IsFalse(left == nullSet);
        Assert.IsFalse(nullSet == left);
        Assert.IsTrue(left != nullSet);
        Assert.IsTrue(nullSet == null);
    }
}

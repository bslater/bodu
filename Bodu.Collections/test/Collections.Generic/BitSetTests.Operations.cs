// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Operations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.And" /> retains only the bits set in both operands.
    /// </summary>
    [TestMethod]
    public void And_WhenOperandsOverlap_ShouldRetainCommonBits()
    {
        var left = CreateWithBits(1, 2, 64, 100);
        var right = CreateWithBits(2, 64, 200);

        left.And(right);

        CollectionAssert.AreEqual(new[] { 2, 64 }, left.ToArray());
        CollectionAssert.AreEqual(new[] { 2, 64, 200 }, right.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.And" /> with a shorter operand clears the bits beyond the operand's capacity
    /// without growing this set.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsShorter_ShouldClearBitsBeyondOtherWithoutGrowing()
    {
        var left = CreateWithBits(1, 500);
        var right = new BitSet(64);
        right.Set(1);
        var capacity = left.Capacity;

        left.And(right);

        CollectionAssert.AreEqual(new[] { 1 }, left.ToArray());
        Assert.AreEqual(capacity, left.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.And" /> with a longer operand never grows this set.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsLonger_ShouldNotGrow()
    {
        var left = new BitSet(64);
        left.Set(1);
        var right = CreateWithBits(1, 5000);

        left.And(right);

        CollectionAssert.AreEqual(new[] { 1 }, left.ToArray());
        Assert.AreEqual(64, left.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.And" /> with the same instance leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsSameInstance_ShouldBeNoOp()
    {
        var bits = CreateWithBits(3, 70);

        bits.And(bits);

        CollectionAssert.AreEqual(new[] { 3, 70 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Or" /> unions the operands, growing to accommodate the other operand's length.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsLonger_ShouldGrowAndUnionBits()
    {
        var left = new BitSet(64);
        left.Set(1);
        var right = CreateWithBits(2, 300);

        left.Or(right);

        CollectionAssert.AreEqual(new[] { 1, 2, 300 }, left.ToArray());
        Assert.IsTrue(left.Capacity >= 301);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Or" /> with a shorter operand preserves this set's high bits.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsShorter_ShouldPreserveHighBits()
    {
        var left = CreateWithBits(500);
        var right = CreateWithBits(1);

        left.Or(right);

        CollectionAssert.AreEqual(new[] { 1, 500 }, left.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Or" /> with the same instance leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsSameInstance_ShouldBeNoOp()
    {
        var bits = CreateWithBits(3, 70);

        bits.Or(bits);

        CollectionAssert.AreEqual(new[] { 3, 70 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Xor" /> produces the symmetric difference, growing to accommodate the other
    /// operand's length.
    /// </summary>
    [TestMethod]
    public void Xor_WhenOperandsOverlap_ShouldProduceSymmetricDifference()
    {
        var left = CreateWithBits(1, 2, 64);
        var right = CreateWithBits(2, 64, 300);

        left.Xor(right);

        CollectionAssert.AreEqual(new[] { 1, 300 }, left.ToArray());
        Assert.IsTrue(left.Capacity >= 301);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Xor" /> with a shorter operand toggles only the shared prefix.
    /// </summary>
    [TestMethod]
    public void Xor_WhenOtherIsShorter_ShouldToggleOnlySharedPrefix()
    {
        var left = CreateWithBits(1, 500);
        var right = CreateWithBits(1, 2);

        left.Xor(right);

        CollectionAssert.AreEqual(new[] { 2, 500 }, left.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Xor" /> with the same instance clears every bit.
    /// </summary>
    [TestMethod]
    public void Xor_WhenOtherIsSameInstance_ShouldClearAllBits()
    {
        var bits = CreateWithBits(3, 70, 500);

        bits.Xor(bits);

        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.AndNot" /> removes the bits set in the other operand.
    /// </summary>
    [TestMethod]
    public void AndNot_WhenOperandsOverlap_ShouldRemoveOtherBits()
    {
        var left = CreateWithBits(1, 2, 64, 100);
        var right = CreateWithBits(2, 100, 300);

        left.AndNot(right);

        CollectionAssert.AreEqual(new[] { 1, 64 }, left.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.AndNot" /> with a longer operand never grows this set.
    /// </summary>
    [TestMethod]
    public void AndNot_WhenOtherIsLonger_ShouldNotGrow()
    {
        var left = new BitSet(64);
        left.Set(1);
        var right = CreateWithBits(1, 5000);

        left.AndNot(right);

        Assert.IsTrue(left.IsEmpty);
        Assert.AreEqual(64, left.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.AndNot" /> with a shorter operand preserves this set's high bits.
    /// </summary>
    [TestMethod]
    public void AndNot_WhenOtherIsShorter_ShouldPreserveHighBits()
    {
        var left = CreateWithBits(1, 500);
        var right = CreateWithBits(1);

        left.AndNot(right);

        CollectionAssert.AreEqual(new[] { 500 }, left.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.AndNot" /> with the same instance clears every bit.
    /// </summary>
    [TestMethod]
    public void AndNot_WhenOtherIsSameInstance_ShouldClearAllBits()
    {
        var bits = CreateWithBits(3, 70);

        bits.AndNot(bits);

        Assert.IsTrue(bits.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Intersects" /> reports a shared set bit and leaves both operands unchanged.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenOperandsShareABit_ShouldReturnTrue()
    {
        var left = CreateWithBits(1, 64);
        var right = CreateWithBits(64, 300);

        Assert.IsTrue(left.Intersects(right));
        Assert.IsTrue(right.Intersects(left));
        CollectionAssert.AreEqual(new[] { 1, 64 }, left.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Intersects" /> returns <see langword="false" /> for disjoint sets and for any
    /// empty operand.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenOperandsAreDisjoint_ShouldReturnFalse()
    {
        var left = CreateWithBits(1, 64);
        var right = CreateWithBits(2, 300);

        Assert.IsFalse(left.Intersects(right));
        Assert.IsFalse(right.Intersects(left));
        Assert.IsFalse(left.Intersects(new BitSet()));
        Assert.IsFalse(new BitSet().Intersects(left));
    }

    /// <summary>
    /// Verifies that each logical operation throws <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// operand.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var bits = new BitSet();

        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => { bits.And(null!); }).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => { bits.Or(null!); }).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => { bits.Xor(null!); }).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => { bits.AndNot(null!); }).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => { _ = bits.Intersects(null!); }).ParamName);
    }
}

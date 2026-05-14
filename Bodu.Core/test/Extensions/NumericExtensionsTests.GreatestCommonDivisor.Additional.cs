// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.GreatestCommonDivisor.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="short"/> returns the expected value for
    /// non-negative inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)0, (short)0, (short)0)]
    [DataRow((short)12, (short)18, (short)6)]
    [DataRow((short)100, (short)75, (short)25)]
    [DataRow((short)7, (short)13, (short)1)]
    public void GreatestCommonDivisor_Short_ShouldReturnExpected(short a, short b, short expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="long"/> returns the expected value for
    /// non-negative inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(0L, 0L, 0L)]
    [DataRow(12L, 18L, 6L)]
    [DataRow(1_000_000_000L, 750_000_000L, 250_000_000L)]
    [DataRow(7L, 13L, 1L)]
    public void GreatestCommonDivisor_Long_ShouldReturnExpected(long a, long b, long expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ushort"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)0, (ushort)0, (ushort)0)]
    [DataRow((ushort)12, (ushort)18, (ushort)6)]
    [DataRow((ushort)1024, (ushort)512, (ushort)512)]
    public void GreatestCommonDivisor_UShort_ShouldReturnExpected(ushort a, ushort b, ushort expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="uint"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0u, 0u, 0u)]
    [DataRow(48u, 18u, 6u)]
    [DataRow(1_000_000_000u, 750_000_000u, 250_000_000u)]
    public void GreatestCommonDivisor_UInt_ShouldReturnExpected(uint a, uint b, uint expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ulong"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0ul, 0ul, 0ul)]
    [DataRow(48ul, 18ul, 6ul)]
    [DataRow(1_000_000_007ul, 1_000_000_009ul, 1ul)]
    public void GreatestCommonDivisor_ULong_ShouldReturnExpected(ulong a, ulong b, ulong expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is commutative for <see cref="int"/> inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(12, 18)]
    [DataRow(7, 13)]
    [DataRow(48, 18)]
    public void GreatestCommonDivisor_Int_ShouldBeCommutative(int a, int b) =>
        Assert.AreEqual(a.GreatestCommonDivisor(b), b.GreatestCommonDivisor(a));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is commutative for <see cref="long"/> inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(12L, 18L)]
    [DataRow(1_000_000_000L, 750_000_000L)]
    public void GreatestCommonDivisor_Long_ShouldBeCommutative(long a, long b) =>
        Assert.AreEqual(a.GreatestCommonDivisor(b), b.GreatestCommonDivisor(a));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor(value, value)</c> equals <c>value</c>.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(12)]
    [DataRow(1_000_000_007)]
    public void GreatestCommonDivisor_Int_WhenInputsAreEqual_ShouldReturnInput(int value) =>
        Assert.AreEqual(value, value.GreatestCommonDivisor(value));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor(value, 1)</c> always returns <c>1</c>.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(48)]
    [DataRow(1_000_000_007)]
    public void GreatestCommonDivisor_Int_WhenOtherIsOne_ShouldReturnOne(int value) =>
        Assert.AreEqual(1, value.GreatestCommonDivisor(1));

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ShortArray_ShouldReturnExpected()
    {
        short[] values = { 24, 36, 60 };
        Assert.AreEqual((short)12, values.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_LongArray_ShouldReturnExpected()
    {
        long[] values = { 1_000_000_000L, 750_000_000L, 500_000_000L };
        Assert.AreEqual(250_000_000L, values.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that the <see cref="ushort"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_UShortArray_ShouldReturnExpected()
    {
        ushort[] values = { 24, 36, 60 };
        Assert.AreEqual((ushort)12, values.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that the <see cref="uint"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_UIntArray_ShouldReturnExpected()
    {
        uint[] values = { 48u, 36u, 60u };
        Assert.AreEqual(12u, values.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that the <see cref="ulong"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ULongArray_ShouldReturnExpected()
    {
        ulong[] values = { 48ul, 36ul, 60ul };
        Assert.AreEqual(12ul, values.GreatestCommonDivisor());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.LeastCommonMultiple.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="short"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)0, (short)0, (short)0)]
    [DataRow((short)4, (short)6, (short)12)]
    [DataRow((short)21, (short)6, (short)42)]
    [DataRow((short)7, (short)13, (short)91)]
    public void LeastCommonMultiple_Short_ShouldReturnExpected(short a, short b, short expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="long"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0L, 0L, 0L)]
    [DataRow(4L, 6L, 12L)]
    [DataRow(21L, 6L, 42L)]
    [DataRow(1_000_000L, 1_500_000L, 3_000_000L)]
    public void LeastCommonMultiple_Long_ShouldReturnExpected(long a, long b, long expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ushort"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)0, (ushort)0, (ushort)0)]
    [DataRow((ushort)4, (ushort)6, (ushort)12)]
    [DataRow((ushort)21, (ushort)6, (ushort)42)]
    public void LeastCommonMultiple_UShort_ShouldReturnExpected(ushort a, ushort b, ushort expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="uint"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0u, 0u, 0u)]
    [DataRow(4u, 6u, 12u)]
    [DataRow(1_000_000u, 1_500_000u, 3_000_000u)]
    public void LeastCommonMultiple_UInt_ShouldReturnExpected(uint a, uint b, uint expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ulong"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0ul, 0ul, 0ul)]
    [DataRow(4ul, 6ul, 12ul)]
    [DataRow(1_000_000_000_000ul, 1_500_000_000_000ul, 3_000_000_000_000ul)]
    public void LeastCommonMultiple_ULong_ShouldReturnExpected(ulong a, ulong b, ulong expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="ArgumentOutOfRangeException"/> when the
    /// right-hand argument is negative.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenRightIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 10.LeastCommonMultiple(-1);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> is commutative.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(4, 6)]
    [DataRow(21, 6)]
    [DataRow(7, 13)]
    public void LeastCommonMultiple_Int_ShouldBeCommutative(int a, int b) =>
        Assert.AreEqual(a.LeastCommonMultiple(b), b.LeastCommonMultiple(a));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple(value, 1)</c> always equals <c>value</c> for positive inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(48)]
    [DataRow(1_000_000)]
    public void LeastCommonMultiple_Int_WhenOtherIsOne_ShouldReturnValue(int value)
    {
        Assert.AreEqual(value, value.LeastCommonMultiple(1));
        Assert.AreEqual(value, 1.LeastCommonMultiple(value));
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple(value, value)</c> equals <c>value</c>.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(12)]
    public void LeastCommonMultiple_Int_WhenInputsAreEqual_ShouldReturnInput(int value) =>
        Assert.AreEqual(value, value.LeastCommonMultiple(value));

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ShortArray_ShouldReturnExpected()
    {
        short[] values = { 4, 6, 8 };
        Assert.AreEqual((short)24, values.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_LongArray_ShouldReturnExpected()
    {
        long[] values = { 4L, 6L, 8L };
        Assert.AreEqual(24L, values.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that the <see cref="ushort"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UShortArray_ShouldReturnExpected()
    {
        ushort[] values = { 4, 6, 8 };
        Assert.AreEqual((ushort)24, values.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that the <see cref="uint"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UIntArray_ShouldReturnExpected()
    {
        uint[] values = { 4u, 6u, 8u };
        Assert.AreEqual(24u, values.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that the <see cref="ulong"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULongArray_ShouldReturnExpected()
    {
        ulong[] values = { 4ul, 6ul, 8ul };
        Assert.AreEqual(24ul, values.LeastCommonMultiple());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.GreatestCommonDivisor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> returns the correct GCD for representative pairs of <see cref="int"/>.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(12, 0, 12)]
    [DataRow(0, 18, 18)]
    [DataRow(12, 18, 6)]
    [DataRow(15, 25, 5)]
    [DataRow(7, 13, 1)]
    [DataRow(48, 18, 6)]
    public void GreatestCommonDivisor_Int_WhenInputsAreNonNegative_ShouldReturnExpectedGcd(int a, int b, int expected)
    {
        int result = a.GreatestCommonDivisor(b);
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> throws <see cref="ArgumentOutOfRangeException"/> for negative left-hand inputs.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenLeftIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = (-1).GreatestCommonDivisor(10);
        });
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> throws <see cref="ArgumentOutOfRangeException"/> for negative right-hand inputs.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenRightIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 10.GreatestCommonDivisor(-1);
        });
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> on an array returns the GCD of every element.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenInputsAreNonNegative_ShouldReturnExpectedGcd()
    {
        int[] values = { 24, 36, 60 };
        Assert.AreEqual(12, values.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that the array overload of <c>GreatestCommonDivisor</c> throws <see cref="ArgumentNullException"/>
    /// when the array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        int[]? values = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = values!.GreatestCommonDivisor();
        });
    }

    /// <summary>
    /// Verifies that the array overload of <c>GreatestCommonDivisor</c> throws <see cref="ArgumentException"/>
    /// when the array is empty.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenArrayIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Array.Empty<int>().GreatestCommonDivisor();
        });
    }

    /// <summary>
    /// Verifies that the array overload of <c>GreatestCommonDivisor</c> throws <see cref="ArgumentOutOfRangeException"/>
    /// when an element is negative.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        int[] values = { 6, -2, 8 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = values.GreatestCommonDivisor();
        });
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ulong"/> works for very large coprime values.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ULong_WhenCoprime_ShouldReturnOne()
    {
        Assert.AreEqual(1UL, 1_000_000_007UL.GreatestCommonDivisor(1_000_000_009UL));
    }
}

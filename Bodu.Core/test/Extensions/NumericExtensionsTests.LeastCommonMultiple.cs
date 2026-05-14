// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.LeastCommonMultiple.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> returns the correct LCM for representative pairs of <see cref="int"/>.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(12, 0, 0)]
    [DataRow(0, 18, 0)]
    [DataRow(4, 6, 12)]
    [DataRow(21, 6, 42)]
    [DataRow(7, 13, 91)]
    [DataRow(8, 12, 24)]
    public void LeastCommonMultiple_Int_WhenInputsAreNonNegative_ShouldReturnExpectedLcm(int a, int b, int expected)
    {
        int result = a.LeastCommonMultiple(b);
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="ArgumentOutOfRangeException"/> for negative inputs.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenLeftIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = (-1).LeastCommonMultiple(10);
        });
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on an array returns the LCM of every element.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_IntArray_WhenInputsAreNonNegative_ShouldReturnExpectedLcm()
    {
        int[] values = { 4, 6, 8 };
        Assert.AreEqual(24, values.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that the array overload of <c>LeastCommonMultiple</c> throws <see cref="ArgumentException"/>
    /// when the array is empty.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_IntArray_WhenArrayIsEmpty_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Array.Empty<int>().LeastCommonMultiple();
        });
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="OverflowException"/> when the result does
    /// not fit in <see cref="int"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenResultOverflows_ShouldThrowOverflowException()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = int.MaxValue.LeastCommonMultiple(int.MaxValue - 1);
        });
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ulong"/> works for moderate values without overflow.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULong_WhenSmallCoprime_ShouldReturnProduct()
    {
        Assert.AreEqual(1_000_000_007UL * 1_000_000_009UL, 1_000_000_007UL.LeastCommonMultiple(1_000_000_009UL));
    }
}

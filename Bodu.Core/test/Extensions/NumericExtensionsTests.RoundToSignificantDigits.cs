// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.RoundToSignificantDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> rounds positive doubles to the requested significant digit count.
    /// </summary>
    [DataTestMethod]
    [DataRow(12345.6789, 3, 12300.0)]
    [DataRow(12345.6789, 5, 12346.0)]
    [DataRow(0.0012345, 2, 0.0012)]
    [DataRow(0.0012345, 3, 0.00123)]
    [DataRow(987.654, 1, 1000.0)]
    [DataRow(987.654, 2, 990.0)]
    public void RoundToSignificantDigits_Double_WhenPositive_ShouldReturnExpected(double value, int digits, double expected)
    {
        double result = value.RoundToSignificantDigits(digits);
        Assert.AreEqual(expected, result, Math.Abs(expected) * 1e-12 + 1e-12);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> rounds symmetrically about zero.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenNegative_ShouldRoundSymmetrically()
    {
        double result = (-12345.6789).RoundToSignificantDigits(3);
        Assert.AreEqual(-12300.0, result, 1e-9);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> returns zero unchanged.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenZero_ShouldReturnZero() =>
        Assert.AreEqual(0.0, 0.0.RoundToSignificantDigits(5));

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> returns non-finite inputs unchanged.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenNonFinite_ShouldReturnInputUnchanged()
    {
        Assert.IsTrue(double.IsNaN(double.NaN.RoundToSignificantDigits(3)));
        Assert.IsTrue(double.IsPositiveInfinity(double.PositiveInfinity.RoundToSignificantDigits(3)));
        Assert.IsTrue(double.IsNegativeInfinity(double.NegativeInfinity.RoundToSignificantDigits(3)));
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> throws when the requested digit count is out of range.
    /// </summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(16)]
    public void RoundToSignificantDigits_Double_WhenDigitsOutOfRange_ShouldThrowArgumentOutOfRangeException(int digits)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 123.45.RoundToSignificantDigits(digits);
        });
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> on <see cref="decimal"/> rounds the value as expected.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenPositive_ShouldReturnExpected()
    {
        decimal result = 12345.6789m.RoundToSignificantDigits(3);
        Assert.AreEqual(12300m, result);
    }

    /// <summary>
    /// Verifies that the decimal overload returns zero unchanged.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenZero_ShouldReturnZero() =>
        Assert.AreEqual(0m, 0m.RoundToSignificantDigits(5));

    /// <summary>
    /// Verifies that the decimal overload throws when the requested digit count is out of range.
    /// </summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(29)]
    public void RoundToSignificantDigits_Decimal_WhenDigitsOutOfRange_ShouldThrowArgumentOutOfRangeException(int digits)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 123.45m.RoundToSignificantDigits(digits);
        });
    }
}

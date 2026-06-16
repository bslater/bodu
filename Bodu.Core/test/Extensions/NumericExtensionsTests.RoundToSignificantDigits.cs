// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.RoundToSignificantDigits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="decimal"/> accepts the maximum valid digit count.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenDigitsIsMaximumValid_ShouldRoundWithoutThrowing()
    {
        decimal value = 1.5m;
        decimal result = value.RoundToSignificantDigits(28);

        Assert.AreEqual(value, result);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="decimal"/> accepts the minimum valid digit count.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenDigitsIsMinimumValid_ShouldRoundToOneDigit() =>
        Assert.AreEqual(100m, 123.45m.RoundToSignificantDigits(1));

    /// <summary>
    /// Verifies that the decimal overload throws when the requested digit count is out of range.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(29)]
    public void RoundToSignificantDigits_Decimal_WhenDigitsOutOfRange_ShouldThrowExactly(int digits) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 123.45m.RoundToSignificantDigits(digits);
        });

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="decimal"/> rounds negative values symmetrically.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenNegative_ShouldRoundSymmetrically()
    {
        decimal result = (-12345.6789m).RoundToSignificantDigits(3);
        Assert.AreEqual(-12300m, result);
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
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="double"/> rounds values midway between
    /// representable significant points away from zero.
    /// </summary>
    [TestMethod]
    [DataRow(1.5, 1, 2.0)]
    [DataRow(2.5, 1, 3.0)]
    [DataRow(-1.5, 1, -2.0)]
    [DataRow(-2.5, 1, -3.0)]
    public void RoundToSignificantDigits_Double_WhenAtMidpoint_ShouldRoundAwayFromZero(double value, int digits, double expected) =>
        Assert.AreEqual(expected, value.RoundToSignificantDigits(digits), 1e-12);

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="double"/> accepts the maximum valid digit
    /// count of <c>15</c>.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenDigitsIsMaximumValid_ShouldRoundWithoutThrowing()
    {
        double value = 1.234567890123456;
        double result = value.RoundToSignificantDigits(15);

        Assert.AreEqual(value, result, 1e-14);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="double"/> accepts the minimum valid digit
    /// count of <c>1</c>.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenDigitsIsMinimumValid_ShouldRoundToOneDigit()
    {
        Assert.AreEqual(100.0, 123.45.RoundToSignificantDigits(1), 1e-9);
        Assert.AreEqual(0.001, 0.00123.RoundToSignificantDigits(1), 1e-9);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> throws when the requested digit count is out of range.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(16)]
    public void RoundToSignificantDigits_Double_WhenDigitsOutOfRange_ShouldThrowExactly(int digits) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 123.45.RoundToSignificantDigits(digits);
        });

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
    /// Verifies that <c>RoundToSignificantDigits</c> rounds positive doubles to the requested significant digit count.
    /// </summary>
    [TestMethod]
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
    /// Verifies that <c>RoundToSignificantDigits</c> returns zero unchanged.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Double_WhenZero_ShouldReturnZero() =>
        Assert.AreEqual(0.0, 0.0.RoundToSignificantDigits(5));

}

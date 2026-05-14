// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.RoundToSignificantDigits.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
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
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="decimal"/> rounds negative values symmetrically.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenNegative_ShouldRoundSymmetrically()
    {
        decimal result = (-12345.6789m).RoundToSignificantDigits(3);
        Assert.AreEqual(-12300m, result);
    }

    /// <summary>
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="decimal"/> accepts the minimum valid digit count.
    /// </summary>
    [TestMethod]
    public void RoundToSignificantDigits_Decimal_WhenDigitsIsMinimumValid_ShouldRoundToOneDigit() =>
        Assert.AreEqual(100m, 123.45m.RoundToSignificantDigits(1));

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
    /// Verifies that <c>RoundToSignificantDigits</c> for <see cref="double"/> rounds values midway between
    /// representable significant points away from zero.
    /// </summary>
    [DataTestMethod]
    [DataRow(1.5, 1, 2.0)]
    [DataRow(2.5, 1, 3.0)]
    [DataRow(-1.5, 1, -2.0)]
    [DataRow(-2.5, 1, -3.0)]
    public void RoundToSignificantDigits_Double_WhenAtMidpoint_ShouldRoundAwayFromZero(double value, int digits, double expected) =>
        Assert.AreEqual(expected, value.RoundToSignificantDigits(digits), 1e-12);
}

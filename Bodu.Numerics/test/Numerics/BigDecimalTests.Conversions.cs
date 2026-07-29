// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that integral types convert implicitly and exactly.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenFromIntegral_ShouldBeExact()
    {
        BigDecimal fromInt = 42;
        BigDecimal fromLong = -7L;
        BigDecimal fromBig = (BigInteger)1000;

        Assert.AreEqual("42", fromInt.ToString());
        Assert.AreEqual("-7", fromLong.ToString());
        Assert.AreEqual("1000", fromBig.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="decimal" /> converts implicitly and exactly, including its scale.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenFromDecimal_ShouldBeExact()
    {
        BigDecimal a = 19.99m;
        BigDecimal b = 0.1m;

        Assert.AreEqual("19.99", a.ToString());
        Assert.AreEqual(new BigDecimal(new BigInteger(1), 1), b);   // exactly 1/10
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.FromDouble" /> uses the shortest round-trip text — so <c>0.1d</c> becomes
    /// exactly <c>0.1</c>, not its binary expansion.
    /// </summary>
    [TestMethod]
    public void FromDouble_WhenGivenDecimalFriendlyValue_ShouldUseRoundTripText()
    {
        Assert.AreEqual(new BigDecimal(new BigInteger(1), 1), BigDecimal.FromDouble(0.1));
        Assert.AreEqual("0.5", BigDecimal.FromDouble(0.5).ToString());
        Assert.AreEqual("-2.75", BigDecimal.FromDouble(-2.75).ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.FromDouble" /> rejects non-finite values.
    /// </summary>
    [TestMethod]
    public void FromDouble_WhenGivenNonFiniteValue_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BigDecimal.FromDouble(double.NaN);
        });
    }

    /// <summary>
    /// Verifies that conversion to <see cref="BigInteger" /> truncates toward zero.
    /// </summary>
    [TestMethod]
    public void ToBigInteger_WhenConverted_ShouldTruncateTowardZero()
    {
        Assert.AreEqual(new BigInteger(3), (BigInteger)BD(35, 1));    // 3.5 -> 3
        Assert.AreEqual(new BigInteger(-3), (BigInteger)BD(-39, 1));  // -3.9 -> -3
        Assert.AreEqual(new BigInteger(100), (BigInteger)BD(100, 0));
    }

    /// <summary>
    /// Verifies that conversion to <see cref="decimal" /> and <see cref="double" /> yields the expected value.
    /// </summary>
    [TestMethod]
    public void ToDecimalAndToDouble_WhenConverted_ShouldYieldExpectedValue()
    {
        Assert.AreEqual(3.14m, (decimal)BD(314, 2));
        Assert.AreEqual(0.5, (double)BD(5, 1));
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.TryToDecimal(out decimal)" /> converts representable values and reports
    /// failure for values beyond the <see cref="decimal" /> range instead of throwing — the non-throwing counterpart
    /// <see cref="Fraction{T}" /> already offers.
    /// </summary>
    [TestMethod]
    public void TryToDecimal_WhenValueExceedsDecimalRange_ShouldReturnFalse()
    {
        Assert.IsTrue(BD(314, 2).TryToDecimal(out decimal small));
        Assert.AreEqual(3.14m, small);

        var huge = new BigDecimal(BigInteger.Pow(10, 40));
        Assert.IsFalse(huge.TryToDecimal(out decimal result));
        Assert.AreEqual(0m, result);
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.TryToDouble(out double)" /> converts representable values and reports
    /// failure for values beyond the finite <see cref="double" /> range instead of returning an infinity.
    /// </summary>
    [TestMethod]
    public void TryToDouble_WhenValueExceedsDoubleRange_ShouldReturnFalse()
    {
        Assert.IsTrue(BD(314, 2).TryToDouble(out double small));
        Assert.AreEqual(3.14, small);

        var huge = new BigDecimal(BigInteger.Pow(10, 400));
        Assert.IsFalse(huge.TryToDouble(out double result));
        Assert.AreEqual(0.0, result);
    }
}

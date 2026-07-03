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
}

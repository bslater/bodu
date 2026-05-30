// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Properties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsoCode" /> returns the currency's ISO code.
    /// </summary>
    [TestMethod]
    public void IsoCode_WhenAccessed_ShouldReturnCurrencyIsoCode()
    {
        Assert.AreEqual("USD", Money<USD>.IsoCode);
        Assert.AreEqual("JPY", Money<JPY>.IsoCode);
        Assert.AreEqual("BHD", Money<BHD>.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.MinorUnits" /> reports the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void MinorUnits_WhenAccessed_ShouldReturnCurrencyMinorUnits()
    {
        Assert.AreEqual(2, Money<USD>.MinorUnits);
        Assert.AreEqual(0, Money<JPY>.MinorUnits);
        Assert.AreEqual(3, Money<BHD>.MinorUnits);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Sign" /> reports the correct sign of the amount.
    /// </summary>
    [TestMethod]
    [DataRow(-5.00, -1)]
    [DataRow(0.0, 0)]
    [DataRow(5.00, 1)]
    public void Sign_WhenAccessed_ShouldReturnAmountSign(double amount, int expectedSign)
    {
        Money<USD> money = new Money<USD>((decimal)amount);

        Assert.AreEqual(expectedSign, money.Sign);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Abs" /> returns the unsigned amount.
    /// </summary>
    [TestMethod]
    public void Abs_WhenAccessed_ShouldReturnAbsoluteAmount()
    {
        Money<USD> negative = new Money<USD>(-19.99m);

        Assert.AreEqual(new Money<USD>(19.99m), negative.Abs);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsZero" /> distinguishes between zero and non-zero amounts.
    /// </summary>
    [TestMethod]
    public void IsZero_WhenAccessed_ShouldReflectAmount()
    {
        Assert.IsTrue(default(Money<USD>).IsZero);
        Assert.IsFalse(new Money<USD>(0.01m).IsZero);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsPositive" /> returns <see langword="true" /> only for amounts
    /// strictly greater than zero.
    /// </summary>
    [TestMethod]
    [DataRow(0.01, true)]
    [DataRow(1234.56, true)]
    [DataRow(0.0, false)]
    [DataRow(-0.01, false)]
    [DataRow(-1234.56, false)]
    public void IsPositive_WhenAccessed_ShouldReflectSign(double amount, bool expected)
    {
        Money<USD> money = new Money<USD>((decimal)amount);

        Assert.AreEqual(expected, money.IsPositive);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsNegative" /> returns <see langword="true" /> only for amounts
    /// strictly less than zero.
    /// </summary>
    [TestMethod]
    [DataRow(-0.01, true)]
    [DataRow(-1234.56, true)]
    [DataRow(0.0, false)]
    [DataRow(0.01, false)]
    [DataRow(1234.56, false)]
    public void IsNegative_WhenAccessed_ShouldReflectSign(double amount, bool expected)
    {
        Money<USD> money = new Money<USD>((decimal)amount);

        Assert.AreEqual(expected, money.IsNegative);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsPositive" /> and <see cref="Money{TCurrency}.IsNegative" />
    /// are both <see langword="false" /> at zero — the boundary value belongs to neither half-line.
    /// </summary>
    [TestMethod]
    public void IsPositiveAndIsNegative_WhenZero_ShouldBothReturnFalse()
    {
        Money<USD> zero = Money<USD>.Zero;

        Assert.IsFalse(zero.IsPositive);
        Assert.IsFalse(zero.IsNegative);
        Assert.IsTrue(zero.IsZero);
    }
}

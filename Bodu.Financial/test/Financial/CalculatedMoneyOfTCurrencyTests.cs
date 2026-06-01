// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyOfTCurrencyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies the strongly-typed <see cref="CalculatedMoney{TCurrency}" /> high-precision deferred-rounding value type.
/// </summary>
[TestClass]
public class CalculatedMoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.ToCalculated" /> preserves the amount.
    /// </summary>
    [TestMethod]
    public void ToCalculated_FromMoney_ShouldPreserveAmount()
    {
        CalculatedMoney<USD> calc = new Money<USD>(12.34m).ToCalculated();

        Assert.AreEqual(12.34m, calc.Amount);
        Assert.AreEqual("USD", CalculatedMoney<USD>.IsoCode);
    }

    /// <summary>
    /// Verifies that arithmetic preserves full precision.
    /// </summary>
    [TestMethod]
    public void Operators_WhenChained_ShouldPreserveFullPrecision()
    {
        CalculatedMoney<USD> value = new(1m);

        Assert.AreEqual(1m / 3m, (value / 3m).Amount);
    }

    /// <summary>
    /// Verifies that deferring rounding yields a more accurate settlement than eager per-step rounding.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenChainDefersRounding_ShouldBeMoreAccurateThanEagerRounding()
    {
        Money<USD> deferred = (new Money<USD>(1m).ToCalculated() / 3m * 3m).RoundToMoney();
        Money<USD> eager = new Money<USD>(1m) / 3m * 3m;

        Assert.AreEqual(new Money<USD>(1.00m), deferred);
        Assert.AreEqual(new Money<USD>(0.99m), eager);
    }

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney{TCurrency}.RoundToMoney(MidpointRounding)" /> honours the supplied
    /// midpoint rule.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenAwayFromZero_ShouldRoundMidpointAway()
    {
        CalculatedMoney<USD> calc = new(1.225m);

        Assert.AreEqual(new Money<USD>(1.23m), calc.RoundToMoney(MidpointRounding.AwayFromZero));
        Assert.AreEqual(new Money<USD>(1.22m), calc.RoundToMoney());
    }
}

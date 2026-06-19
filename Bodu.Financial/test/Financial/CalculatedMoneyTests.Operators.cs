// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that arithmetic preserves full precision rather than rounding to minor units.
    /// </summary>
    [TestMethod]
    public void Operators_WhenChained_ShouldPreserveFullPrecision()
    {
        CalculatedMoney value = new(1m, CurrencyCode.USD);

        CalculatedMoney result = value / 3m;

        Assert.AreEqual(1m / 3m, result.Amount);
    }

    /// <summary>
    /// Verifies that every arithmetic operator on same-currency operands computes the full-precision result without
    /// rounding.
    /// </summary>
    [TestMethod]
    public void Operators_WhenSameCurrency_ShouldComputeFullPrecisionResults()
    {
        CalculatedMoney a = new(10m, CurrencyCode.USD);
        CalculatedMoney b = new(3m, CurrencyCode.USD);

        Assert.AreEqual(new CalculatedMoney(13m, CurrencyCode.USD), a + b);
        Assert.AreEqual(new CalculatedMoney(7m, CurrencyCode.USD), a - b);
        Assert.AreEqual(new CalculatedMoney(-10m, CurrencyCode.USD), -a);
        Assert.AreEqual(a, +a);
        Assert.AreEqual(new CalculatedMoney(20m, CurrencyCode.USD), a * 2m);
        Assert.AreEqual(new CalculatedMoney(20m, CurrencyCode.USD), 2m * a);
        Assert.AreEqual(new CalculatedMoney(5m, CurrencyCode.USD), a / 2m);
    }
}

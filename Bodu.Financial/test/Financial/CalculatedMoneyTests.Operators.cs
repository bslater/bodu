// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that arithmetic preserves full precision rather than rounding to minor units.
    /// </summary>
    [TestMethod]
    public void Operators_WhenChained_ShouldPreserveFullPrecision()
    {
        CalculatedMoney value = new(1m, "USD");

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
        CalculatedMoney a = new(10m, "USD");
        CalculatedMoney b = new(3m, "USD");

        Assert.AreEqual(new CalculatedMoney(13m, "USD"), a + b);
        Assert.AreEqual(new CalculatedMoney(7m, "USD"), a - b);
        Assert.AreEqual(new CalculatedMoney(-10m, "USD"), -a);
        Assert.AreEqual(a, +a);
        Assert.AreEqual(new CalculatedMoney(20m, "USD"), a * 2m);
        Assert.AreEqual(new CalculatedMoney(20m, "USD"), 2m * a);
        Assert.AreEqual(new CalculatedMoney(5m, "USD"), a / 2m);
    }
}

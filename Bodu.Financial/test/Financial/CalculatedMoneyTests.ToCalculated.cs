// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.ToCalculated.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that <see cref="Money.ToCalculated" /> preserves the amount and ISO code.
    /// </summary>
    [TestMethod]
    public void ToCalculated_FromMoney_ShouldPreserveAmountAndIsoCode()
    {
        CalculatedMoney calc = new Money(12.34m, CurrencyCode.USD).ToCalculated();

        Assert.AreEqual(12.34m, calc.Amount);
        Assert.AreEqual(CurrencyCode.USD, calc.Code);
    }

    /// <summary>
    /// Verifies that <see cref="Money.ToCalculated" /> throws when the source carries no currency.
    /// </summary>
    [TestMethod]
    public void ToCalculated_FromDefaultMoney_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = default(Money).ToCalculated());
    }
}

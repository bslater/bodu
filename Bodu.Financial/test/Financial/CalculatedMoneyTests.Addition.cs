// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Addition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that addition across different currencies throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        CalculatedMoney usd = new(1m, CurrencyCode.USD);
        CalculatedMoney eur = new(1m, CurrencyCode.EUR);

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = usd + eur);
    }
}

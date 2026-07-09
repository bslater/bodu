// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.Convert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.Convert" /> multiplies the source amount by the rate and
    /// rounds to the destination currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Convert_WhenCalledOnTypedAmount_ShouldReturnTypedQuoteAmount()
    {
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);
        var amount = new Money<USD>(100m);

        Money<AUD> result = rate.Convert(amount);

        Assert.AreEqual(152.00m, result.Amount);
    }
}

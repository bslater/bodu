// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.MoneyOfTCurrencyConvert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TQuote}(ExchangeRate{TCurrency, TQuote}, MidpointRounding)" />
    /// delegates to the typed rate's <c>Convert</c> and returns a value in the rate's quote currency.
    /// </summary>
    [TestMethod]
    public void MoneyOfTCurrencyConvert_WhenSuppliedTypedRate_ShouldReturnTypedQuoteAmount()
    {
        var amount = new Money<USD>(50m);
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        Money<AUD> result = amount.Convert(rate);

        Assert.AreEqual(76.00m, result.Amount);
    }
}

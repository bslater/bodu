// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that the typed exchange rate exposes the ISO codes of <typeparamref name="TBase" /> and
    /// <typeparamref name="TQuote" /> on its <see cref="ExchangeRate{TBase, TQuote}.FromIsoCode" /> and
    /// <see cref="ExchangeRate{TBase, TQuote}.ToIsoCode" /> properties.
    /// </summary>
    [TestMethod]
    public void Properties_WhenConstructed_ShouldDeriveIsoCodesFromTypeParameters()
    {
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        Assert.AreEqual("USD", rate.FromIsoCode);
        Assert.AreEqual("AUD", rate.ToIsoCode);
        Assert.AreEqual(1.52m, rate.Rate);
        Assert.AreEqual(SampleDate, rate.Date);
        Assert.AreEqual(SampleProvider, rate.Provider);
        Assert.IsFalse(rate.IsInverted);
    }
}

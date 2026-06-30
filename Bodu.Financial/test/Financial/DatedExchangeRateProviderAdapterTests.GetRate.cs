// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderAdapterTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class DatedExchangeRateProviderAdapterTests
{

    /// <summary>
    /// Verifies that the adapter forwards lookups to the inner dated provider with the pinned date and options, and
    /// returns the raw rate value through <see cref="IExchangeRateProvider.GetRate" />.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasRate_ShouldReturnRateValue()
    {
        FixedDatedExchangeRateProvider inner = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.50m, "RBA"),
        ]);
        DatedExchangeRateProviderAdapter adapter = new(inner, s_d1, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(1.50m, adapter.GetRate("USD", "AUD"));
    }

    /// <summary>
    /// Verifies that the adapter propagates exceptions thrown by the inner provider — for example, when no rate is
    /// available under the pinned date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasNoRate_ShouldThrowKeyNotFoundException()
    {
        FixedDatedExchangeRateProvider inner = new([]);
        DatedExchangeRateProviderAdapter adapter = new(inner, s_d1, ExchangeRateLookupOptions.Exact);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => adapter.GetRate("USD", "AUD"));
    }
}

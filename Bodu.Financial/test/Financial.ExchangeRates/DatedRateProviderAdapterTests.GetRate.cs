// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderAdapterTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class DatedRateProviderAdapterTests
{

    /// <summary>
    /// Verifies that the adapter forwards lookups to the inner dated provider with the pinned date and options, and
    /// returns the raw rate value through <see cref="IRateProvider.GetRate" />.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasRate_ShouldReturnRateValue()
    {
        FixedDatedRateProvider inner = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.50m, "RBA"),
        ]);
        DatedRateProviderAdapter adapter = new(inner, s_d1, RateLookupOptions.Exact);

        Assert.AreEqual(1.50m, adapter.GetRate("USD", "AUD"));
    }

    /// <summary>
    /// Verifies that the adapter propagates exceptions thrown by the inner provider — for example, when no rate is
    /// available under the pinned date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasNoRate_ShouldThrowKeyNotFoundException()
    {
        FixedDatedRateProvider inner = new([]);
        DatedRateProviderAdapter adapter = new(inner, s_d1, RateLookupOptions.Exact);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => adapter.GetRate("USD", "AUD"));
    }
}

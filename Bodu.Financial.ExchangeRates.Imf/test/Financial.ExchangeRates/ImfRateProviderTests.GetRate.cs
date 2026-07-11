// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a USD-to-quote lookup resolves to the report's USD-anchored rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUsdToQuote_ShouldResolveDirectRate()
    {
        using ImfRateProvider provider = await CreateLoadedProviderAsync();

        RateLookupResult result = provider.GetRate("USD", "JPY", s_seeded, RateLookupOptions.Exact);

        Assert.AreEqual(158.84m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a quote-to-USD lookup resolves to the reciprocal of the direct rate through the base
    /// inverse-lookup fallback.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenQuoteToUsd_ShouldResolveReciprocalRate()
    {
        using ImfRateProvider provider = await CreateLoadedProviderAsync();

        decimal direct = provider.GetRate("USD", "JPY", s_seeded, RateLookupOptions.Exact).Rate.Rate;
        decimal inverse = provider.GetRate("JPY", "USD", s_seeded, RateLookupOptions.Exact).Rate.Rate;

        Assert.IsTrue(direct > 0m && inverse > 0m);
        Assert.IsLessThan(0.0001m, Math.Abs((direct * inverse) - 1m));
    }
}

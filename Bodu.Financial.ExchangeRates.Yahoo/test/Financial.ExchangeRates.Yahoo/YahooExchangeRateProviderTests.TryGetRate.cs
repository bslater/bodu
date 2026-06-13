// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a lookup against an un-fetched pair returns <see langword="false" /> when synchronous network
    /// access is disabled.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSyncDisabledAndNotLoaded_ShouldReturnFalse()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: false);

        var found = provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(0, source.GetChartCallCount);
    }

    /// <summary>
    /// Verifies that a lookup against an un-fetched pair fetches a window on demand when synchronous network access is
    /// enabled, then resolves the rate.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSyncEnabledAndNotLoaded_ShouldFetchOnDemand()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: true);

        var found = provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 6), ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1, source.GetChartCallCount);
        Assert.AreEqual(0.6855m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a same-currency lookup resolves the identity rate without any network access.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrency_ShouldReturnIdentityWithoutFetch()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: true);

        var found = provider.TryGetRate("USD", "USD", new DateOnly(2023, 1, 6), ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1m, result.Rate.Rate);
        Assert.AreEqual(0, source.GetChartCallCount);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the behaviour of <see cref="YahooExchangeRateProvider" /> against fixture-backed chart data.
/// </summary>
[TestClass]
public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Creates a provider backed by the fixture source, optionally allowing synchronous on-demand fetching.
    /// </summary>
    /// <param name="allowSync">Whether synchronous network access is permitted.</param>
    /// <returns>The provider and its fixture source.</returns>
    private static (YahooExchangeRateProvider Provider, FixtureYahooExchangeRateChartSource Source) Create(bool allowSync = true)
    {
        YahooExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = allowSync };
        FixtureYahooExchangeRateChartSource source = new(options);
        return (new YahooExchangeRateProvider(source, options), source);
    }

    /// <summary>
    /// Creates a provider with the AUD/USD pair already loaded for January 2023.
    /// </summary>
    /// <returns>The preloaded provider.</returns>
    private static async Task<YahooExchangeRateProvider> CreatePreloadedAsync()
    {
        (YahooExchangeRateProvider provider, _) = Create(allowSync: false);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        return provider;
    }

    /// <summary>
    /// Verifies that a loaded pair resolves the published rate for an exact date.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task GetRate_WhenLoaded_ShouldReturnPublishedRate()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(YahooExchangeRateProvider.ProviderName, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the discovered-pairs snapshot includes a loaded pair with its ticker.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenPairLoaded_ShouldIncludeTicker()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        YahooSeriesInfo info = provider.GetAvailablePairs().Single();

        Assert.AreEqual(CurrencyCode.AUD, info.Pair.From);
        Assert.AreEqual(CurrencyCode.USD, info.Pair.To);
        Assert.AreEqual("AUDUSD=X", info.Symbol);
    }
}

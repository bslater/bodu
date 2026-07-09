// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the behaviour of <see cref="YahooRateProvider" /> against fixture-backed chart data.
/// </summary>
[TestClass]
public partial class YahooRateProviderTests
{
    /// <summary>
    /// Creates a provider backed by the fixture source, optionally allowing synchronous on-demand fetching.
    /// </summary>
    /// <param name="allowSync">Whether synchronous network access is permitted.</param>
    /// <returns>The provider and its fixture source.</returns>
    private static (YahooRateProvider Provider, FixtureYahooRateSource Source) Create(bool allowSync = true)
    {
        YahooRateProviderOptions options = new() { AllowSynchronousNetworkAccess = allowSync };
        FixtureYahooRateSource source = new(options);
        return (new YahooRateProvider(source, options), source);
    }

    /// <summary>
    /// Creates a provider with the AUD/USD pair already loaded for January 2023.
    /// </summary>
    /// <returns>The preloaded provider.</returns>
    private static async Task<YahooRateProvider> CreatePreloadedAsync()
    {
        (YahooRateProvider provider, _) = Create(allowSync: false);
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
        YahooRateProvider provider = await CreatePreloadedAsync();

        RateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(YahooRateProvider.ProviderName, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the discovered-pairs snapshot includes a loaded pair with its ticker.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenPairLoaded_ShouldIncludeTicker()
    {
        YahooRateProvider provider = await CreatePreloadedAsync();

        YahooSeriesInfo info = provider.GetAvailablePairs().Single();

        Assert.AreEqual(CurrencyCode.AUD, info.Pair.From);
        Assert.AreEqual(CurrencyCode.USD, info.Pair.To);
        Assert.AreEqual("AUDUSD=X", info.Symbol);
    }
}

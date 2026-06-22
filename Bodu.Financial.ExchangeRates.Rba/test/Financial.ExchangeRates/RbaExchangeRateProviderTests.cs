// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Financial.Currencies;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the lookup behavior of <see cref="RbaExchangeRateProvider" /> using an offline fixture source.
/// </summary>
[TestClass]
public partial class RbaExchangeRateProviderTests
{
    /// <summary>
    /// Creates a provider backed by the fixture source, optionally allowing synchronous on-demand loading.
    /// </summary>
    /// <param name="allowSync">Whether to allow synchronous on-demand loading.</param>
    /// <returns>The provider and its fixture source.</returns>
    private static (RbaExchangeRateProvider Provider, FixtureRbaExchangeRateTableSource Source) Create(bool allowSync = true)
    {
        RbaExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = allowSync, EnableDiskCache = false };
        FixtureRbaExchangeRateTableSource source = new(options);
        return (new RbaExchangeRateProvider(source, options), source);
    }

    /// <summary>
    /// Creates a provider whose 2023-to-current era has been preloaded.
    /// </summary>
    /// <returns>The preloaded provider.</returns>
    private static async Task<RbaExchangeRateProvider> CreatePreloadedAsync()
    {
        (RbaExchangeRateProvider provider, _) = Create(allowSync: false);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 12, 31));
        return provider;
    }

    /// <summary>
    /// Verifies that a dated lookup against preloaded data returns the published rate.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task GetRate_WhenLoaded_ShouldReturnPublishedRate()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(RbaExchangeRateProvider.ProviderName, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the reverse direction is served by inverting the AUD-based series.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenInverseRequested_ShouldReturnInvertedRate()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("USD", "AUD", new DateOnly(2023, 1, 3));

        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(1m / 0.6828m, result.Rate.Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that a same-currency lookup returns the synthetic identity rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenSameCurrency_ShouldReturnIdentityRate()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("USD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a synchronous lookup blocks to load the covering era on demand when permitted.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenEraNotLoadedAndSyncEnabled_ShouldLazyLoadAndResolve()
    {
        (RbaExchangeRateProvider provider, FixtureRbaExchangeRateTableSource source) = Create(allowSync: true);

        bool found = provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(1, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a synchronous lookup reports a miss without loading when on-demand loading is disabled.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenEraNotLoadedAndSyncDisabled_ShouldReturnFalse()
    {
        (RbaExchangeRateProvider provider, FixtureRbaExchangeRateTableSource source) = Create(allowSync: false);

        bool found = provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), null, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(0, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that the undated provider surface returns the most recent available rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndated_ShouldReturnMostRecentRate()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        decimal latest = provider.GetRate("AUD", "USD").Rate.Rate;

        Assert.AreEqual(0.7029m, latest);
    }

    /// <summary>
    /// Verifies that the discovered pairs include AUD/USD after loading.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenLoaded_ShouldIncludeAudUsd()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        var pairs = provider.GetAvailablePairs().Select(p => p.Pair).ToList();

        CollectionAssert.Contains(pairs, new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD));
    }
}

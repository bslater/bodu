// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the lookup behavior of <see cref="EcbRateProvider" /> using an offline fixture source.
/// </summary>
[TestClass]
public partial class EcbRateProviderTests
{
    /// <summary>
    /// Creates a provider backed by the fixture source, optionally allowing synchronous on-demand loading.
    /// </summary>
    /// <param name="allowSync">Whether to allow synchronous on-demand loading.</param>
    /// <returns>The provider and its fixture source.</returns>
    private static (EcbRateProvider Provider, FixtureEcbRateTableSource Source) Create(bool allowSync = true)
    {
        EcbRateProviderOptions options = new() { AllowSynchronousNetworkAccess = allowSync, EnableDiskCache = false };
        FixtureEcbRateTableSource source = new(options);
        return (new EcbRateProvider(source, options), source);
    }

    /// <summary>
    /// Creates a provider whose full-history feed has been preloaded.
    /// </summary>
    /// <returns>The preloaded provider.</returns>
    private static async Task<EcbRateProvider> CreatePreloadedAsync()
    {
        (EcbRateProvider provider, _) = Create(allowSync: false);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        return provider;
    }

    /// <summary>
    /// Verifies that a dated lookup against preloaded data returns the published euro reference rate.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task GetRate_WhenLoaded_ShouldReturnPublishedRate()
    {
        EcbRateProvider provider = await CreatePreloadedAsync();

        RateLookupResult result = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1.0545m, result.Rate.Rate);
        Assert.AreEqual(EcbRateProvider.ProviderName, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the reverse direction is served by inverting the EUR-based series.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenInverseRequested_ShouldReturnInvertedRate()
    {
        EcbRateProvider provider = await CreatePreloadedAsync();

        RateLookupResult result = provider.GetRate("USD", "EUR", new DateOnly(2023, 1, 3));

        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(1m / 1.0545m, result.Rate.Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that a same-currency lookup returns the synthetic identity rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenSameCurrency_ShouldReturnIdentityRate()
    {
        EcbRateProvider provider = await CreatePreloadedAsync();

        RateLookupResult result = provider.GetRate("USD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a synchronous lookup blocks to load the covering feed on demand when permitted.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFeedNotLoadedAndSyncEnabled_ShouldLazyLoadAndResolve()
    {
        (EcbRateProvider provider, FixtureEcbRateTableSource source) = Create(allowSync: true);

        bool found = provider.TryGetRate("EUR", "USD", new DateOnly(2023, 1, 3), null, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.0545m, result.Rate.Rate);
        Assert.AreEqual(1, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a synchronous lookup reports a miss without loading when on-demand loading is disabled.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFeedNotLoadedAndSyncDisabled_ShouldReturnFalse()
    {
        (EcbRateProvider provider, FixtureEcbRateTableSource source) = Create(allowSync: false);

        bool found = provider.TryGetRate("EUR", "USD", new DateOnly(2023, 1, 3), null, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(0, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that the undated provider surface returns the most recent available rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndated_ShouldReturnMostRecentRate()
    {
        EcbRateProvider provider = await CreatePreloadedAsync();

        decimal latest = provider.GetRate("EUR", "USD").Rate.Rate;

        Assert.AreEqual(1.0600m, latest);
    }

    /// <summary>
    /// Verifies that the discovered pairs include EUR/USD after loading.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenLoaded_ShouldIncludeEurUsd()
    {
        EcbRateProvider provider = await CreatePreloadedAsync();

        var pairs = provider.GetAvailablePairs().Select(p => p.Pair).ToList();

        CollectionAssert.Contains(pairs, new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD));
    }
}

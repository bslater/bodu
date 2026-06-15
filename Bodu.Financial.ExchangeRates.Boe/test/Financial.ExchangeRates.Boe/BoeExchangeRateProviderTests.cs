// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies the lookup behavior of <see cref="BoeExchangeRateProvider" /> using an offline fixture source.
/// </summary>
[TestClass]
public partial class BoeExchangeRateProviderTests
{
    /// <summary>
    /// Creates a provider backed by the fixture source, optionally allowing synchronous on-demand loading.
    /// </summary>
    /// <param name="allowSync">Whether to allow synchronous on-demand loading.</param>
    /// <returns>The provider and its fixture source.</returns>
    private static (BoeExchangeRateProvider Provider, FixtureBoeExchangeRateTableSource Source) Create(bool allowSync = true)
    {
        BoeExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = allowSync, EnableDiskCache = false };
        FixtureBoeExchangeRateTableSource source = new(options);
        return (new BoeExchangeRateProvider(source, options), source);
    }

    /// <summary>
    /// Creates a provider whose 2023 range has been preloaded.
    /// </summary>
    /// <returns>The preloaded provider.</returns>
    private static async Task<BoeExchangeRateProvider> CreatePreloadedAsync()
    {
        (BoeExchangeRateProvider provider, _) = Create(allowSync: false);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        return provider;
    }

    /// <summary>
    /// Verifies that a dated lookup against preloaded data returns the published spot rate.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task GetRate_WhenLoaded_ShouldReturnPublishedRate()
    {
        BoeExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1.2065m, result.Rate.Rate);
        Assert.AreEqual(BoeExchangeRateProvider.ProviderName, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the reverse direction is served by inverting the GBP-based series.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenInverseRequested_ShouldReturnInvertedRate()
    {
        BoeExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("USD", "GBP", new DateOnly(2023, 1, 3));

        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(1m / 1.2065m, result.Rate.Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that a same-currency lookup returns the synthetic identity rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenSameCurrency_ShouldReturnIdentityRate()
    {
        BoeExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = provider.GetRate("USD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a synchronous lookup blocks to load a window around the date on demand when permitted.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRangeNotLoadedAndSyncEnabled_ShouldLazyLoadAndResolve()
    {
        (BoeExchangeRateProvider provider, FixtureBoeExchangeRateTableSource source) = Create(allowSync: true);

        var found = provider.TryGetRate("GBP", "USD", new DateOnly(2023, 1, 3), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.2065m, result.Rate.Rate);
        Assert.AreEqual(1, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a synchronous lookup reports a miss without loading when on-demand loading is disabled.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRangeNotLoadedAndSyncDisabled_ShouldReturnFalse()
    {
        (BoeExchangeRateProvider provider, FixtureBoeExchangeRateTableSource source) = Create(allowSync: false);

        var found = provider.TryGetRate("GBP", "USD", new DateOnly(2023, 1, 3), null, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(0, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that the undated provider surface returns the most recent available rate.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndated_ShouldReturnMostRecentRate()
    {
        BoeExchangeRateProvider provider = await CreatePreloadedAsync();

        var latest = provider.GetRate("GBP", "USD").Rate.Rate;

        Assert.AreEqual(1.2050m, latest);
    }

    /// <summary>
    /// Verifies that a range already covered by a previous load is not downloaded again.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenAlreadyCovered_ShouldNotReload()
    {
        (BoeExchangeRateProvider provider, FixtureBoeExchangeRateTableSource source) = Create(allowSync: false);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        await provider.LoadRangeAsync(new DateOnly(2023, 2, 1), new DateOnly(2023, 6, 30));

        Assert.AreEqual(1, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that the discovered pairs include GBP/USD after loading.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenLoaded_ShouldIncludeGbpUsd()
    {
        BoeExchangeRateProvider provider = await CreatePreloadedAsync();

        var pairs = provider.GetAvailablePairs().Select(p => p.Pair).ToList();

        CollectionAssert.Contains(pairs, new ExchangeRatePair("GBP", "USD"));
    }
}

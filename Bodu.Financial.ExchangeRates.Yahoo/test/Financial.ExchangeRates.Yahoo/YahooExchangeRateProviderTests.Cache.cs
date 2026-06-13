// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.Cache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that fresh cached rates spanning the requested window are served without any network fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenCacheIsFreshAndCoversRange_ShouldServeFromCacheWithoutFetch()
    {
        YahooExchangeRateOptions options = new() { CacheExpiry = TimeSpan.FromHours(24) };
        FixtureYahooExchangeRateChartSource source = new(options);
        InMemoryYahooRateCache cache = new();
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.Store(pair, YahooExchangeRateProvider.ProviderName, new[]
        {
            new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now),
            new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now),
        });

        YahooExchangeRateProvider provider = new(source, options, cache);

        IReadOnlyList<ExchangeRate> rates =
            await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(0, source.GetChartCallCount);
        Assert.AreEqual(0.5000m, rates[0].Rate);
    }

    /// <summary>
    /// Verifies that expired cached rates are ignored and the provider re-fetches.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenCacheIsExpired_ShouldFetch()
    {
        YahooExchangeRateOptions options = new() { CacheExpiry = TimeSpan.FromHours(24) };
        FixtureYahooExchangeRateChartSource source = new(options);
        InMemoryYahooRateCache cache = new();
        ExchangeRatePair pair = new("AUD", "USD");
        var stale = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);

        cache.Store(pair, YahooExchangeRateProvider.ProviderName, new[]
        {
            new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, stale),
        });

        YahooExchangeRateProvider provider = new(source, options, cache);

        ExchangeRateLookupResult result = (await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31)))
            .Count > 0
            ? provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3))
            : default;

        Assert.AreEqual(1, source.GetChartCallCount);
        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that fetched rates are persisted to the cache, each stamped with a retrieval time.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFetched_ShouldPersistRatesToCache()
    {
        YahooExchangeRateOptions options = new();
        FixtureYahooExchangeRateChartSource source = new(options);
        InMemoryYahooRateCache cache = new();
        ExchangeRatePair pair = new("AUD", "USD");
        var before = DateTimeOffset.UtcNow;

        YahooExchangeRateProvider provider = new(source, options, cache);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        IReadOnlyList<CachedExchangeRate> cached = cache.GetRates(pair, YahooExchangeRateProvider.ProviderName);

        Assert.AreEqual(3, cached.Count);
        Assert.IsTrue(cached.All(r => r.RetrievedAt >= before));
    }
}

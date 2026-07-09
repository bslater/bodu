// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that when recorded coverage contains the requested range, the range is served from the cached rows
    /// without fetching.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCoverageContainsRange_ShouldServeWithoutFetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        SeedCache(pair, (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        SeedCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        CachingRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6))];

        Assert.HasCount(2, rates);
        Assert.AreEqual(0.5m, rates[0].Rate);
        Assert.AreEqual(0, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that a range miss fetches from the source and caches the result so a repeat request is served without a
    /// second fetch.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCacheMiss_ShouldFetchThenServeRepeatFromCache()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> first = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6))];
        IReadOnlyList<ExchangeRate> second = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6))];

        Assert.HasCount(2, first);
        Assert.HasCount(2, second);
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that a request wider than the cached span refetches the whole range.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeWiderThanCachedSpan_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 10), 0.52m));
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10))];

        Assert.HasCount(2, rates);
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that a range whose cached rows have expired is refetched.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCacheStale_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        CachingRateProvider sut = CreateDecorator(inner);

        _clock.Advance(Duration + TimeSpan.FromHours(1));
        IReadOnlyList<ExchangeRate> rates = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6))];

        Assert.HasCount(2, rates);
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that an inverted range is rejected.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        CachingRateProvider sut = CreateDecorator(InnerWith());

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 6), new DateOnly(2023, 1, 3));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that when the fresh cached rows span the requested range, the range is served without fetching.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCacheSpansRange_ShouldServeWithoutFetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(2, rates.Count);
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
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> first = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        IReadOnlyList<ExchangeRate> second = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(2, second.Count);
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
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10));

        Assert.AreEqual(2, rates.Count);
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
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        _clock.Advance(Duration + TimeSpan.FromHours(1));
        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(2, rates.Count);
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that an inverted range is rejected.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        CachingDatedExchangeRateProvider sut = CreateDecorator(InnerWith());

        var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 6), new DateOnly(2023, 1, 3));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a range the first source cannot satisfy falls through to the next source.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenFirstSourceEmpty_ShouldFallThroughToNext()
    {
        CountingDatedExchangeRateProvider first = InnerWith();
        CountingDatedExchangeRateProvider second = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.6m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(Source("First", first), Source("Second", second));

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual(0.6m, rates[0].Rate);
        Assert.AreEqual(1, first.GetRatesAsyncCallCount);
        Assert.AreEqual(1, second.GetRatesAsyncCallCount);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.RangeCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a range straddling an interior day that was never fetched is not served from the sparse cached rows
    /// and is instead refetched, even though the cached rows' min and max dates span the requested window.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeStraddlesUnfetchedGap_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 5), 0.505m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        ExchangeRatePair pair = new("AUD", "USD");

        // Seed rows that span 3..6 and coverage for only the disjoint ends 3..3 and 6..6: the interior of 4..5 was never
        // fetched, so a 3..6 request must not be served from the cache.
        SeedCache(pair, (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        SeedCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));
        SeedCoverage(pair, new DateOnly(2023, 1, 6), new DateOnly(2023, 1, 6));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
        Assert.AreEqual(3, rates.Count);
    }

    /// <summary>
    /// Verifies that after a range is fetched and its coverage recorded, a sub-window wholly inside that coverage is
    /// served from the cache without a second fetch.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSubWindowOfRecordedCoverage_ShouldServeWithoutFetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 4), 0.50m),
            ("AUD", "USD", new DateOnly(2023, 1, 5), 0.505m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        IReadOnlyList<ExchangeRate> sub = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 5));

        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
        Assert.AreEqual(2, sub.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 4), sub[0].Date);
        Assert.AreEqual(new DateOnly(2023, 1, 5), sub[1].Date);
    }

    /// <summary>
    /// Verifies that once the recorded coverage has expired, a previously cached range is refetched even though the rows
    /// remain present, because coverage freshness — not row presence — governs range serving.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRecordedCoverageExpired_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        _clock.Advance(Duration + TimeSpan.FromHours(1));
        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(2, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that a fetch returning zero rows still records the window as covered, so the same range is served from
    /// the cache on the next call without a second fetch — the empty-but-fetched fix.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenFetchReturnsNoRows_ShouldRecordCoverageAndNotRefetch()
    {
        // The inner source has no observations in the requested window, so the fetch returns an empty list.
        CountingDatedExchangeRateProvider inner = InnerWith();
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> first = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        IReadOnlyList<ExchangeRate> second = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(0, first.Count);
        Assert.AreEqual(0, second.Count);

        // The empty window was recorded as covered on the first call, so the second call is a cache hit returning an
        // empty list rather than a second fetch.
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
    }

    /// <summary>
    /// Verifies that when the cache's atomic write reports failure (nothing persisted), the decorator does not treat the
    /// range as covered: an identical later lookup refetches from the inner provider rather than serving an empty list
    /// as a false hit.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenStoreFetchedRangeFails_ShouldRefetchRatherThanFalseHit()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));

        // A cache whose StoreFetchedRange swallows the write and reports Failed, and whose reads return nothing: the
        // coverage was never persisted, so a range lookup must miss and refetch.
        var failingCache = new FailingStoreExchangeRateCache(Provider);
        var sut = new CachingExchangeRateProvider(inner, failingCache, _options, _clock);

        IReadOnlyList<ExchangeRate> first = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        IReadOnlyList<ExchangeRate> second = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(2, second.Count);

        // Neither call was served from a false hit: both reached the inner provider because coverage was never recorded.
        Assert.AreEqual(2, inner.GetRatesAsyncCallCount);
        Assert.AreEqual(2, failingCache.StoreFetchedRangeCallCount);
    }

    /// <summary>
    /// Verifies that a backend modelling a swallowed storage write reports <see cref="ExchangeRateCacheWriteStatus.Failed" />
    /// from <see cref="IExchangeRateCache.StoreFetchedRange" />, the contract signal the decorator relies on to refetch
    /// rather than trust coverage that was never persisted.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenBackendSwallowsWrite_ShouldReportFailed()
    {
        var failingCache = new FailingStoreExchangeRateCache(Provider);
        var now = _clock.GetUtcNow();

        ExchangeRateCacheWriteStatus status = failingCache.StoreFetchedRange(
            new ExchangeRatePair("AUD", "USD"),
            new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3),
            Duration,
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Failed, status);
        Assert.AreEqual(1, failingCache.StoreFetchedRangeCallCount);
    }
}

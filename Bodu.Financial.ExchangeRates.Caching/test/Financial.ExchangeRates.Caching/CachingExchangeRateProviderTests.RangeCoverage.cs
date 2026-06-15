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
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.SnapshotRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the range serve path's snapshot read: a backend exposing the snapshot seam is read once per consulted pair,
/// while a backend without the seam is served through the standard coverage-then-rows fallback.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The pair used by the snapshot-read tests.</summary>
    private static readonly CurrencyPair SnapshotPair = new(CurrencyCode.EUR, CurrencyCode.USD);

    /// <summary>
    /// Verifies that a covered range hit against a snapshot-capable backend performs exactly one snapshot read and no
    /// separate coverage or rate reads.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenRangeCoveredAndCacheSupportsSnapshots_ShouldReadStateOnce()
    {
        var cache = new CountingSnapshotRateCache(Provider);
        var start = new DateOnly(2023, 1, 2);
        var end = new DateOnly(2023, 1, 6);
        cache.StoreFetchedRange(
            SnapshotPair,
            new[] { new CachedRate(start, 1.1m, Now), new CachedRate(end, 1.2m, Now) },
            start,
            end,
            Duration,
            Now);
        var decorator = new CachingRateProvider(InnerWith(), cache, _options, _clock);

        IReadOnlyList<ExchangeRate> rates = [.. decorator.GetRates("EUR", "USD", start, end)];

        Assert.AreEqual(2, rates.Count);
        Assert.AreEqual(1, cache.SnapshotCount);
        Assert.AreEqual(0, cache.GetCoverageCount);
        Assert.AreEqual(0, cache.GetRatesCount);
    }

    /// <summary>
    /// Verifies that a covered range hit against a backend without the snapshot seam is served through the standard
    /// coverage-then-rows fallback.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenRangeCoveredAndCacheLacksSnapshots_ShouldServeThroughFallback()
    {
        var cache = new CountingRateCache(Provider);
        var start = new DateOnly(2023, 1, 2);
        var end = new DateOnly(2023, 1, 6);
        cache.StoreFetchedRange(
            SnapshotPair,
            new[] { new CachedRate(start, 1.1m, Now), new CachedRate(end, 1.2m, Now) },
            start,
            end,
            Duration,
            Now);
        var decorator = new CachingRateProvider(InnerWith(), cache, _options, _clock);

        IReadOnlyList<ExchangeRate> rates = [.. decorator.GetRates("EUR", "USD", start, end)];

        Assert.AreEqual(2, rates.Count);
        Assert.AreEqual(1, cache.GetCoverageCount);
        Assert.AreEqual(1, cache.GetRatesCount);
    }

    /// <summary>
    /// Verifies that an inverse-covered range against a snapshot-capable backend reads each consulted pair's state once
    /// and serves the reciprocated rates.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenOnlyInverseCoveredAndCacheSupportsSnapshots_ShouldReadEachPairOnce()
    {
        var cache = new CountingSnapshotRateCache(Provider);
        var start = new DateOnly(2023, 1, 2);
        var end = new DateOnly(2023, 1, 6);
        cache.StoreFetchedRange(
            SnapshotPair.Inverse(),
            new[] { new CachedRate(start, 2.0m, Now) },
            start,
            end,
            Duration,
            Now);
        var decorator = new CachingRateProvider(InnerWith(), cache, _options, _clock);

        IReadOnlyList<ExchangeRate> rates = [.. decorator.GetRates("EUR", "USD", start, end)];

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual(0.5m, rates[0].Rate);
        Assert.IsTrue(rates[0].IsInverted);
        Assert.AreEqual(2, cache.SnapshotCount);
        Assert.AreEqual(0, cache.GetCoverageCount);
        Assert.AreEqual(0, cache.GetRatesCount);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.FetchedAtUtc.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingRateProvider" /> preserves the inner provider's per-load fetch instant
/// (<see cref="ExchangeRate.FetchedAtUtc" />) end to end: it is stamped onto the served rate on a miss, persisted in the
/// cache, and restored onto the served rate on a subsequent hit rather than being replaced by the cache-write instant.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// The upstream fetch instant carried by the inner provider's stamped series; deliberately earlier than
    /// <see cref="Now" /> so it is distinguishable from the cache-write instant.
    /// </summary>
    private static readonly DateTimeOffset FetchedAt = new(2023, 1, 2, 6, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds an inner provider over a book whose single AUD/USD series carries <see cref="FetchedAt" /> as its fetch
    /// instant, so a served rate can be attributed to when its backing data was downloaded.
    /// </summary>
    /// <returns>A fixed provider backed by the stamped series.</returns>
    private static FixedDatedRateProvider StampedInner()
    {
        RateTableBuilder builder = new();
        builder.Upsert(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), Provider, new DateOnly(2023, 1, 3), 0.5m, FetchedAt);
        builder.Upsert(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), Provider, new DateOnly(2023, 1, 6), 0.51m, FetchedAt);
        return new FixedDatedRateProvider(builder.ToBook());
    }

    /// <summary>
    /// Verifies that a single-date cache miss serves a rate stamped with the inner provider's fetch instant.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenMiss_ShouldServeRateStampedWithInnerFetchInstant()
    {
        CachingRateProvider sut = CreateDecorator(StampedInner());

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(FetchedAt, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a single-date cache hit, served after the miss cached the row, still reports the inner provider's
    /// original fetch instant on the served rate rather than the later cache-write instant.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenHitAfterMiss_ShouldRestoreInnerFetchInstantNotCacheWriteInstant()
    {
        CachingRateProvider sut = CreateDecorator(StampedInner());

        // First call misses and caches the row, stamping ObservedAtUtc from the inner fetch instant.
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        // Advance the clock so the cache-write instant (Now) is clearly distinct from a later hit.
        _clock.Advance(TimeSpan.FromHours(2));
        RateLookupResult hit = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(FetchedAt, hit.Rate.FetchedAtUtc);
        Assert.AreNotEqual(Now, hit.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that on a single-date cache hit the served provenance age is anchored to the cache-write instant, so the
    /// rate's upstream fetch instant and the provenance age are independent measures.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenHitAfterMiss_ShouldAnchorProvenanceAgeToCacheWriteInstant()
    {
        CachingRateProvider sut = CreateDecorator(StampedInner());
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        _clock.Advance(TimeSpan.FromHours(2));
        RateLookupResult hit = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(RateOrigin.Cache, hit.Provenance.Origin);

        // The cache-write instant is Now; the hit is two hours later, so the provenance age is two hours — independent
        // of the much older upstream fetch instant carried on the rate.
        Assert.AreEqual(TimeSpan.FromHours(2), hit.Provenance.Age);
        Assert.AreEqual(Now, hit.Provenance.CachedAtUtc);
    }

    /// <summary>
    /// Verifies that a range cache miss serves rates stamped with the inner provider's fetch instant.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenMiss_ShouldServeRatesStampedWithInnerFetchInstant()
    {
        CachingRateProvider sut = CreateDecorator(StampedInner());

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.HasCount(2, rates);
        Assert.IsTrue(rates.All(r => r.FetchedAtUtc == FetchedAt), "every refetched rate should carry the inner fetch instant");
    }

    /// <summary>
    /// Verifies that a range cache hit, served after the miss cached the window, still reports the inner provider's
    /// original fetch instant on every served rate rather than the later cache-write instant.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenHitAfterMiss_ShouldRestoreInnerFetchInstantOnEveryRate()
    {
        CachingRateProvider sut = CreateDecorator(StampedInner());

        // First range call misses and caches both rows and the covered window.
        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        _clock.Advance(TimeSpan.FromHours(2));
        IReadOnlyList<ExchangeRate> hit = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        Assert.HasCount(2, hit);
        Assert.IsTrue(hit.All(r => r.FetchedAtUtc == FetchedAt), "every cached rate should restore the inner fetch instant");
        Assert.IsTrue(hit.All(r => r.FetchedAtUtc != Now), "the cache-write instant must not leak onto the served rate");
    }
}

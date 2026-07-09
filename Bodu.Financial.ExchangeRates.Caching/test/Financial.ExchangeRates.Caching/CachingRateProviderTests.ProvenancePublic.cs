// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.ProvenancePublic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingRateProvider" /> populates the public
/// <see cref="RateLookupResult.Provenance" /> on the returned result: a cache serve carries cache lineage while
/// a miss carries the inner provider's live lineage unchanged.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that a single-date cache hit returns a result whose provenance reports <c>Cache</c> origin, the
    /// in-memory cache backend, the instant the served row was cached, and an age equal to the elapsed time since it
    /// was cached.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheHit_ShouldReturnCacheProvenanceWithBackendAndAge()
    {
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(InnerWith(), _cache, _options, _clock);

        _clock.Advance(TimeSpan.FromHours(1));
        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(RateOrigin.Cache, result.Provenance.Origin);
        Assert.AreEqual(Provider, result.Provenance.Provider);
        Assert.AreEqual(nameof(InMemoryRateCache), result.Provenance.Backend);
        Assert.AreEqual(Now, result.Provenance.CachedAtUtc);
        Assert.AreEqual(TimeSpan.FromHours(1), result.Provenance.Age);
    }

    /// <summary>
    /// Verifies that a single-date cache hit served through the asynchronous surface returns the same cache provenance
    /// as the synchronous surface: <c>Cache</c> origin, the in-memory cache backend, the instant the served row was
    /// cached, and an age equal to the elapsed time since it was cached.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenCacheHit_ShouldReturnCacheProvenanceWithBackendAndAge()
    {
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(InnerWith(), _cache, _options, _clock);

        _clock.Advance(TimeSpan.FromHours(1));
        RateLookupResult result = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(RateOrigin.Cache, result.Provenance.Origin);
        Assert.AreEqual(Provider, result.Provenance.Provider);
        Assert.AreEqual(nameof(InMemoryRateCache), result.Provenance.Backend);
        Assert.AreEqual(Now, result.Provenance.CachedAtUtc);
        Assert.AreEqual(TimeSpan.FromHours(1), result.Provenance.Age);
    }

    /// <summary>
    /// Verifies that a single-date cache miss resolved from the inner source returns a result whose provenance reports
    /// <c>Live</c> origin attributed to the inner source, with no cache backend, cache instant, or age.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCacheMiss_ShouldReturnLiveProvenanceWithNoCacheFields()
    {
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(inner, _cache, _options, _clock);

        Assert.IsTrue(sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result));

        Assert.AreEqual(RateOrigin.Live, result.Provenance.Origin);
        Assert.AreEqual("Test", result.Provenance.Provider);
        Assert.IsNull(result.Provenance.Backend);
        Assert.IsNull(result.Provenance.CachedAtUtc);
        Assert.IsNull(result.Provenance.Age);
    }

    /// <summary>
    /// Verifies that a row cached marginally in the future of the lookup instant — within the cache's clock-skew
    /// tolerance, so it is still served — reports a clamped <see cref="TimeSpan.Zero" /> age rather than a negative one.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCachedRowStampedSlightlyInFuture_ShouldClampAgeToZero()
    {
        // Stamp the row 30 seconds ahead of the lookup clock: within the one-minute skew tolerance, so it remains valid
        // and fresh, but its raw age (asOf - cachedAtUtc) is negative and must clamp to zero.
        DateTimeOffset futureStamp = Now + TimeSpan.FromSeconds(30);
        _cache.Store(
            new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD),
            [new CachedRate(new DateOnly(2023, 1, 3), 0.5m, futureStamp)],
            Duration,
            futureStamp);

        CachingRateProvider sut = new(InnerWith(), _cache, _options, _clock);

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(RateOrigin.Cache, result.Provenance.Origin);
        Assert.AreEqual(futureStamp, result.Provenance.CachedAtUtc);
        Assert.AreEqual(TimeSpan.Zero, result.Provenance.Age);
    }
}

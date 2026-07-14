// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.AsyncCachePath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the decorator's routing between the synchronous cache surface and the internal asynchronous seam: the
/// asynchronous provider surfaces use only the seam when the cache exposes one, the synchronous surfaces never do,
/// and the caller's cancellation token flows through to the seam.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that a single-date async lookup — miss then hit — performs all cache I/O through the async seam and
    /// never through the synchronous members.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenCacheExposesAsyncSeam_ShouldUseOnlyAsyncMembers()
    {
        var cache = new CountingAsyncRateCache(Provider);
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)), cache, _options, _clock);

        _ = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        RateLookupResult hit = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0, cache.SyncCalls, "the async surface must not touch the synchronous cache members");
        Assert.IsTrue(cache.AsyncCalls > 0, "the async surface must route through the async seam");
        Assert.AreEqual(RateOrigin.Cache, hit.Provenance.Origin, "the second lookup must be a cache hit");
    }

    /// <summary>
    /// Verifies that a range async lookup — refetch then covered serve — performs all cache I/O through the async
    /// seam.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCacheExposesAsyncSeam_ShouldUseOnlyAsyncMembers()
    {
        var cache = new CountingAsyncRateCache(Provider);
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)), cache, _options, _clock);

        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));
        RateRangeResult hit = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));

        Assert.AreEqual(0, cache.SyncCalls, "the async surface must not touch the synchronous cache members");
        Assert.IsTrue(cache.AsyncCalls > 0, "the async surface must route through the async seam");
        Assert.HasCount(1, hit.Rates);
    }

    /// <summary>
    /// Verifies that the synchronous surfaces never call the asynchronous seam, so the two routes stay cleanly
    /// separated.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheExposesAsyncSeam_ShouldUseOnlySyncMembers()
    {
        var cache = new CountingAsyncRateCache(Provider);
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)), cache, _options, _clock);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0, cache.AsyncCalls, "the synchronous surface must not touch the async seam");
        Assert.IsTrue(cache.SyncCalls > 0);
    }

    /// <summary>
    /// Verifies that the caller's cancellation token flows through to the async seam.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenTokenSupplied_ShouldFlowTokenToTheSeam()
    {
        var cache = new CountingAsyncRateCache(Provider);
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)), cache, _options, _clock);
        using var cts = new CancellationTokenSource();

        _ = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, cts.Token);

        Assert.AreEqual(cts.Token, cache.LastToken, "the caller's token must reach the seam");
    }

    /// <summary>
    /// Verifies that both surfaces observe one another's writes: a synchronous store is served by the asynchronous
    /// surface and vice versa, because both routes share one state and one per-pair lock.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenStoredSynchronously_ShouldServeAcrossSurfaces()
    {
        var cache = new CountingAsyncRateCache(Provider);
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)), cache, _options, _clock);

        // Miss on the sync surface stores the row; the async surface must then hit it.
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        RateLookupResult hit = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(RateOrigin.Cache, hit.Provenance.Origin, "an async read must observe the synchronous write");
    }
}

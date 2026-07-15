// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.Async.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheTests
{
    /// <summary>
    /// Verifies end to end that the caching provider's asynchronous surfaces drive the distributed backend through
    /// its asynchronous store APIs — no synchronous Get or Set — so network I/O never blocks a thread-pool thread.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenBackedByDistributedCache_ShouldUseAsyncStoreApis()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions { Provider = Provider });
        var clock = new Bodu.Test.Time.MutableTimeProvider(now);
        var inner = new FixedDatedRateProvider(BuildBook(new DateOnly(2023, 1, 3), 0.5m));
        var provider = new CachingRateProvider(inner, cache, new CachingRateOptions { DefaultExpiry = Duration }, clock);

        _ = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));
        RateRangeResult hit = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        Assert.AreEqual(0, backing.GetCalls, "the async surface must not use the synchronous Get");
        Assert.AreEqual(0, backing.SetCalls, "the async surface must not use the synchronous Set");
        Assert.IsTrue(backing.GetAsyncCalls > 0, "reads must flow through GetAsync");
        Assert.IsTrue(backing.SetAsyncCalls > 0, "writes must flow through SetAsync");
        Assert.HasCount(1, hit.Rates);
    }

    /// <summary>
    /// Verifies that a row stored through the asynchronous route is served by the synchronous route, and that the
    /// asynchronous write stamps the same server-side lifetime as the synchronous one.
    /// </summary>
    [TestMethod]
    public async Task GetRates_WhenStoredThroughAsyncRoute_ShouldServeAcrossSurfacesWithStampedLifetime()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions { Provider = Provider });
        var clock = new Bodu.Test.Time.MutableTimeProvider(now);
        var inner = new FixedDatedRateProvider(BuildBook(new DateOnly(2023, 1, 3), 0.5m));
        var provider = new CachingRateProvider(inner, cache, new CachingRateOptions { DefaultExpiry = Duration }, clock);

        _ = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        Assert.HasCount(1, backing.Sets);
        Assert.AreEqual(Duration + TimeSpan.FromHours(1), backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow, "the async write must stamp the same server-side lifetime");
        Assert.HasCount(1, cache.GetRates(Pair, Duration, now), "the synchronous surface must observe the asynchronous write");
    }

    /// <summary>
    /// Verifies that a cancelled token surfaces as <see cref="OperationCanceledException" /> from the asynchronous
    /// surface rather than being swallowed as a failed cache operation.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var cache = new DistributedRateCache(new RecordingDistributedCache(), new DistributedRateCacheOptions { Provider = Provider });
        var clock = new Bodu.Test.Time.MutableTimeProvider(now);
        var inner = new FixedDatedRateProvider(BuildBook(new DateOnly(2023, 1, 3), 0.5m));
        var provider = new CachingRateProvider(inner, cache, new CachingRateOptions { DefaultExpiry = Duration }, clock);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3), cts.Token);
        });
    }

    /// <summary>
    /// Builds a single-pair rate book for the async tests.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    /// <returns>The frozen book.</returns>
    private static RateBook BuildBook(DateOnly date, decimal rate)
    {
        RateTableBuilder builder = new();
        builder.Upsert(Pair, Provider, date, rate);
        return builder.ToBook();
    }
}

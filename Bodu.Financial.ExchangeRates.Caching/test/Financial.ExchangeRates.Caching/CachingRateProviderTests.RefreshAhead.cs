// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.RefreshAhead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test.Diagnostics;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the refresh-ahead (stale-while-revalidate) contract across the decorator's serve surfaces: an aged hit is
/// served immediately and schedules at most one background refetch, the refreshed rows reset the entry's freshness, a
/// failing refresh is swallowed, and the default configuration schedules nothing.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The pair the refresh-ahead tests serve.</summary>
    private static readonly CurrencyPair RefreshPair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that a hit whose served data is younger than the refresh-ahead threshold schedules no background
    /// refetch.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenServedBelowRefreshThreshold_ShouldNotScheduleRefresh()
    {
        _options.RefreshAheadFraction = 0.5;
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(TimeSpan.FromHours(6));

        bool served = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.IsTrue(served, "sanity: the lookup must be a cache hit");
        Assert.AreEqual(0, inner.TotalCallCount, "a young hit must not trigger any inner fetch");
    }

    /// <summary>
    /// Verifies that a hit whose served data has aged past the refresh-ahead threshold is still served from the cache
    /// and additionally refetches the date once in the background, resetting the stored row's freshness.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenServedPastRefreshThreshold_ShouldRefetchOnceInBackground()
    {
        _options.RefreshAheadFraction = 0.5;
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.75m));
        CachingRateProvider sut = CreateDecorator(inner);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(TimeSpan.FromHours(13));

        bool served = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.IsTrue(served);
        Assert.AreEqual(0.5m, result.Rate.Rate, "the aged hit must serve the cached value, not the refreshed one");
        Assert.AreEqual(1, inner.GetRateCallCount, "exactly one background refetch must run");

        IReadOnlyList<CachedRate> rows = _cache.GetRates(RefreshPair, Duration, _clock.GetUtcNow());
        Assert.HasCount(1, rows);
        Assert.AreEqual(_clock.GetUtcNow(), rows[0].CachedAtUtc, "the refresh must restamp the row at the refresh instant");
        Assert.AreEqual(0.75m, rows[0].Rate, "the refreshed row must carry the inner provider's current rate");
    }

    /// <summary>
    /// Verifies that the asynchronous single-date surface schedules the same background refetch as the synchronous
    /// one.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenServedPastRefreshThreshold_ShouldRefetchOnceInBackground()
    {
        _options.RefreshAheadFraction = 0.5;
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.75m));
        CachingRateProvider sut = CreateDecorator(inner);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(TimeSpan.FromHours(13));

        RateLookupResult result = await sut.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(0.5m, result.Rate.Rate, "the aged hit must serve the cached value");
        Assert.AreEqual(1, inner.GetRateCallCount, "exactly one background refetch must run");
    }

    /// <summary>
    /// Verifies that an aged covered-range hit is served from the cache and refetches the window once in the
    /// background, restamping both the rows and the coverage.
    /// </summary>
    [TestMethod]
    public async Task GetRates_WhenRangePastRefreshThreshold_ShouldRefetchWindowInBackground()
    {
        _options.RefreshAheadFraction = 0.5;
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 4);
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", start, 0.75m), ("AUD", "USD", end, 0.76m));
        CachingRateProvider sut = CreateDecorator(inner);
        SeedCache(RefreshPair, (start, 0.5m), (end, 0.51m));
        SeedCoverage(RefreshPair, start, end);
        _clock.Advance(TimeSpan.FromHours(13));

        RateRangeResult result = sut.GetRates("AUD", "USD", start, end);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.HasCount(2, result.Rates);
        Assert.AreEqual(0.5m, result.Rates[0].Rate, "the aged hit must serve the cached values");
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "exactly one background window refetch must run");

        IReadOnlyList<CachedRate> rows = _cache.GetRates(RefreshPair, Duration, _clock.GetUtcNow());
        Assert.HasCount(2, rows);
        Assert.AreEqual(_clock.GetUtcNow(), rows[0].CachedAtUtc, "the refresh must restamp the rows at the refresh instant");
        Assert.IsTrue(
            _cache.GetCoverage(RefreshPair, Duration, _clock.GetUtcNow()).Contains(start, end),
            "the refreshed coverage must still contain the window");
    }

    /// <summary>
    /// Verifies that with the default options an aged hit — served just before expiry — schedules no background work
    /// and behaves exactly as before refresh-ahead existed.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenRefreshAheadDisabled_ShouldNotRefetchAgedHit()
    {
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.75m));
        CachingRateProvider sut = CreateDecorator(inner);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(Duration - TimeSpan.FromHours(1));

        bool served = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.IsTrue(served);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(0, inner.TotalCallCount, "the default configuration must never schedule background work");
    }

    /// <summary>
    /// Verifies that aged hits arriving while a refresh for the same key is still in flight join the pending refresh
    /// rather than scheduling duplicates.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenConcurrentAgedHits_ShouldRefetchOnlyOnce()
    {
        _options.RefreshAheadFraction = 0.5;
        var gated = new GatedDatedRateProvider(new[]
        {
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2023, 1, 3), 0.75m, Provider),
        });
        CachingRateProvider sut = CreateDecorator(gated);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(TimeSpan.FromHours(13));

        // The first aged hit schedules the refresh, which blocks on the gate; the later hits must join it.
        for (int i = 0; i < 5; i++)
            Assert.IsTrue(sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _));

        gated.Open();
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, gated.SingleDateCalls, "the aged hits must coalesce onto one background refetch");
    }

    /// <summary>
    /// Verifies that a failing background refresh is swallowed: the triggering hit was already served, the failure is
    /// counted with the failed outcome, and a later lookup still serves from the cache.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenRefreshFails_ShouldSwallowAndCountFailedOutcome()
    {
        string provider = $"refresh-{Guid.NewGuid():N}";
        using var collector = new TestMeterCollector(CachingMeter.MeterName);
        _options.RefreshAheadFraction = 0.5;
        var cache = new InMemoryRateCache(provider);
        CountingDatedRateProvider inner = InnerWith();
        var sut = new CachingRateProvider(inner, cache, _options, _clock);
        cache.Store(RefreshPair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, _clock.GetUtcNow()) }, Duration, _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromHours(13));

        bool served = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.IsTrue(served, "the aged hit must be served even though its refresh will fail");
        Assert.AreEqual(1, SumRefreshAhead(collector, provider, "failed"), "the failed refresh must be counted");
        Assert.IsTrue(sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _), "the cached row must still serve after the failed refresh");
    }

    /// <summary>
    /// Verifies that a successful background refresh is counted with the success outcome.
    /// </summary>
    [TestMethod]
    public async Task TryGetRate_WhenRefreshSucceeds_ShouldCountSuccessOutcome()
    {
        string provider = $"refresh-{Guid.NewGuid():N}";
        using var collector = new TestMeterCollector(CachingMeter.MeterName);
        _options.RefreshAheadFraction = 0.5;
        var cache = new InMemoryRateCache(provider);
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.75m));
        var sut = new CachingRateProvider(inner, cache, _options, _clock);
        cache.Store(RefreshPair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, _clock.GetUtcNow()) }, Duration, _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromHours(13));

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        await sut.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, SumRefreshAhead(collector, provider, "success"), "the successful refresh must be counted");
    }

    /// <summary>
    /// Verifies that disposing the decorator while a refresh is in flight neither throws nor breaks the pending
    /// refresh, which completes quietly.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenRefreshPending_ShouldNotThrowAndPendingRefreshCompletes()
    {
        _options.RefreshAheadFraction = 0.5;
        var gated = new GatedDatedRateProvider(new[]
        {
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2023, 1, 3), 0.75m, Provider),
        });
        CachingRateProvider sut = CreateDecorator(gated);
        SeedCache(RefreshPair, (new DateOnly(2023, 1, 3), 0.5m));
        _clock.Advance(TimeSpan.FromHours(13));

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        Task pending = sut.WaitForRefreshAheadForTestingAsync();

        sut.Dispose();
        gated.Open();
        await pending;
    }

    /// <summary>
    /// Sums the refresh-ahead counter for one provider and outcome.
    /// </summary>
    /// <param name="collector">The collector observing the meter.</param>
    /// <param name="provider">The unique provider name the test is bound to.</param>
    /// <param name="outcome">The outcome tag to filter on.</param>
    /// <returns>The summed count.</returns>
    private static long SumRefreshAhead(TestMeterCollector collector, string provider, string outcome) =>
        collector.Measurements("bodu.financial.rate_cache.refresh_ahead")
            .Where(m => m.Tags.Any(t => t.Key == "provider" && Equals(t.Value, provider))
                && m.Tags.Any(t => t.Key == "outcome" && Equals(t.Value, outcome)))
            .Sum(m => m.Value);
}

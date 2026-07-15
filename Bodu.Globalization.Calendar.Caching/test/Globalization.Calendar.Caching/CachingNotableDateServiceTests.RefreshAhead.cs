// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.RefreshAhead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the refresh-ahead (stale-while-revalidate) contract of <see cref="CachingNotableDateService" />: an aged
/// hit is served immediately and schedules at most one background recompute, the recomputed entry resets freshness,
/// the batch read path schedules too, a failing recompute is swallowed, and the default configuration schedules
/// nothing.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>The time-to-live used by the refresh-ahead tests.</summary>
    private static readonly TimeSpan RefreshTtl = TimeSpan.FromDays(30);

    /// <summary>
    /// Builds a caching service with refresh-ahead enabled at the supplied fraction over the supplied inner service.
    /// </summary>
    /// <param name="time">The clock the service measures against.</param>
    /// <param name="fraction">The refresh-ahead fraction to configure.</param>
    /// <param name="inner">The inner service computing on a miss.</param>
    /// <param name="cache">The in-memory cache backing the service.</param>
    /// <returns>The caching service.</returns>
    private static CachingNotableDateService BuildRefreshAheadService(TimeProvider time, double fraction, INotableDateService inner, InMemoryNotableDateCache cache) =>
        new(
            inner,
            cache,
            new NotableDateCachingOptions { Ttl = RefreshTtl, ResourceVersion = "fixed", RefreshAheadFraction = fraction },
            timeProvider: time);

    /// <summary>
    /// Verifies that a hit whose served entry is younger than the refresh-ahead threshold schedules no background
    /// recompute.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenServedBelowRefreshThreshold_ShouldNotScheduleRecompute()
    {
        var time = new MutableTimeProvider(Now);
        var inner = new CountingNotableDateService();
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, inner, new InMemoryNotableDateCache());

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        time.Advance(TimeSpan.FromDays(10));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, inner.ResolveCount, "a young hit must not trigger a background recompute");
    }

    /// <summary>
    /// Verifies that a hit whose served entry has aged past the refresh-ahead threshold is still served from the
    /// cache and additionally recomputes the year once in the background, restamping the cached entry.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenServedPastRefreshThreshold_ShouldRecomputeOnceInBackground()
    {
        var time = new MutableTimeProvider(Now);
        var inner = new CountingNotableDateService();
        var cache = new InMemoryNotableDateCache();
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, inner, cache);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        time.Advance(TimeSpan.FromDays(20));
        IReadOnlyList<NotableDate> served = service.Resolve(new DateOnly(2026, 1, 1), "US");
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.HasCount(1, served, "the aged hit must be served from the cache");
        Assert.AreEqual(2, inner.ResolveCount, "exactly one background recompute must run beyond the initial miss");

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "fixed", RefreshTtl, time.GetUtcNow());
        Assert.IsNotNull(entry);
        Assert.AreEqual(time.GetUtcNow(), entry.ComputedAtUtc, "the recompute must restamp the entry at the refresh instant");
    }

    /// <summary>
    /// Verifies that with the default options an aged hit — served just before expiry — schedules no background work
    /// and behaves exactly as before refresh-ahead existed.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenRefreshAheadDisabled_ShouldNotRecomputeAgedHit()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, RefreshTtl, out CountingNotableDateService inner, out _);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        time.Advance(RefreshTtl - TimeSpan.FromDays(1));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, inner.ResolveCount, "the default configuration must never schedule background work");
    }

    /// <summary>
    /// Verifies that aged hits arriving while a recompute for the same year is still in flight join the pending
    /// recompute rather than scheduling duplicates.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenConcurrentAgedHits_ShouldRecomputeOnlyOnce()
    {
        var time = new MutableTimeProvider(Now);
        var gated = new GatedNotableDateService();
        var cache = new InMemoryNotableDateCache();
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, gated, cache);

        _ = cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "fixed", time.GetUtcNow()), RefreshTtl, time.GetUtcNow());
        time.Advance(TimeSpan.FromDays(20));

        // The first aged hit schedules the recompute, which blocks on the gate; the later hits must join it.
        for (int i = 0; i < 5; i++)
            _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        gated.Open();
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, gated.ResolveCount, "the aged hits must coalesce onto one background recompute");
    }

    /// <summary>
    /// Verifies that an aged multi-year hit served through the batch read path schedules a background recompute for
    /// each served year.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenBatchPathServesAgedYears_ShouldRecomputeEachYearInBackground()
    {
        var time = new MutableTimeProvider(Now);
        var inner = new CountingNotableDateService();
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, inner, new InMemoryNotableDateCache());
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31));

        _ = service.Resolve(range, "US");
        Assert.AreEqual(2, inner.ResolveCount, "sanity: the cold span must compute both years");

        time.Advance(TimeSpan.FromDays(20));
        _ = service.Resolve(range, "US");
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(4, inner.ResolveCount, "each aged year served by the batch path must recompute once");
    }

    /// <summary>
    /// Verifies that a failing background recompute is swallowed: the triggering hit was already served, the failure
    /// is counted with the failed outcome, and the cached entry keeps serving.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenRecomputeFails_ShouldSwallowAndCountFailedOutcome()
    {
        string territory = UniqueTerritory();
        using var collector = new TestMeterCollector(CalendarCachingMeter.MeterName);
        var time = new MutableTimeProvider(Now);
        var gated = new GatedNotableDateService(throwOnFirstCall: true);
        var cache = new InMemoryNotableDateCache();
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, gated, cache);
        gated.Open();

        _ = cache.StoreYear(NotableDateCacheTestFactory.Entry(territory, 2026, "fixed", time.GetUtcNow()), RefreshTtl, time.GetUtcNow());
        time.Advance(TimeSpan.FromDays(20));

        IReadOnlyList<NotableDate> served = service.Resolve(new DateOnly(2026, 1, 1), territory);
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.HasCount(1, served, "the aged hit must be served even though its recompute will fail");
        Assert.AreEqual(1, SumRefreshAhead(collector, territory, "failed"), "the failed recompute must be counted");
        Assert.HasCount(1, service.Resolve(new DateOnly(2026, 1, 1), territory), "the cached entry must keep serving after the failed recompute");
    }

    /// <summary>
    /// Verifies that a successful background recompute is counted with the success outcome.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenRecomputeSucceeds_ShouldCountSuccessOutcome()
    {
        string territory = UniqueTerritory();
        using var collector = new TestMeterCollector(CalendarCachingMeter.MeterName);
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildRefreshAheadService(time, 0.5, new CountingNotableDateService(), new InMemoryNotableDateCache());

        _ = service.Resolve(new DateOnly(2026, 1, 1), territory);
        time.Advance(TimeSpan.FromDays(20));
        _ = service.Resolve(new DateOnly(2026, 1, 1), territory);
        await service.WaitForRefreshAheadForTestingAsync();

        Assert.AreEqual(1, SumRefreshAhead(collector, territory, "success"), "the successful recompute must be counted");
    }

    /// <summary>
    /// Sums the refresh-ahead counter for one territory and outcome.
    /// </summary>
    /// <param name="collector">The collector observing the meter.</param>
    /// <param name="territory">The unique normalized territory the test is bound to.</param>
    /// <param name="outcome">The outcome tag to filter on.</param>
    /// <returns>The summed count.</returns>
    private static long SumRefreshAhead(TestMeterCollector collector, string territory, string outcome) =>
        collector.Measurements("bodu.calendar.notable_date_cache.refresh_ahead")
            .Where(m => m.Tags.Any(t => t.Key == "territory" && Equals(t.Value, territory))
                && m.Tags.Any(t => t.Key == "outcome" && Equals(t.Value, outcome)))
            .Sum(m => m.Value);
}

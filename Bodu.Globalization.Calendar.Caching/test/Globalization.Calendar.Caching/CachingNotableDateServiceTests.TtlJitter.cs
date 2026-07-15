// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.TtlJitter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>The configured time-to-live the jitter tests shorten.</summary>
    private static readonly TimeSpan JitterTtl = TimeSpan.FromDays(30);

    /// <summary>
    /// Builds a caching service with the supplied jitter fraction over a counting inner and an in-memory cache.
    /// </summary>
    /// <param name="clock">The controllable clock.</param>
    /// <param name="fraction">The jitter fraction to configure.</param>
    /// <param name="inner">The counting inner service.</param>
    /// <returns>The caching service.</returns>
    private static CachingNotableDateService BuildJitteredService(
        MutableTimeProvider clock,
        double fraction,
        out CountingNotableDateService inner)
    {
        inner = new CountingNotableDateService();
        return new CachingNotableDateService(
            inner,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = JitterTtl, TtlJitter = fraction, ResourceVersion = "fixed" },
            timeProvider: clock);
    }

    /// <summary>
    /// Verifies that with jitter enabled a cached year expires at the territory's jittered instant — earlier than the
    /// configured time-to-live — and recomputes.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTtlJitterEnabled_ShouldRecomputeAtTheJitteredInstant()
    {
        TimeSpan effective = Bodu.Caching.CacheFreshness.WithJitter(JitterTtl, "US", 0.5);
        Assert.IsTrue(effective < JitterTtl, "the jitter must shorten the effective time-to-live for this key");

        var clock = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildJitteredService(clock, 0.5, out CountingNotableDateService inner);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        Assert.AreEqual(1, inner.ResolveCount);

        // Just inside the jittered time-to-live: still a hit.
        clock.Advance(effective - TimeSpan.FromMinutes(1));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        Assert.AreEqual(1, inner.ResolveCount, "just inside the jittered time-to-live must still be a hit");

        // Past the jittered instant but well inside the configured time-to-live: recomputed.
        clock.Advance(TimeSpan.FromMinutes(2));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        Assert.AreEqual(2, inner.ResolveCount, "past the jittered time-to-live must recompute even though the configured one has not elapsed");
    }

    /// <summary>
    /// Verifies that the batch (multi-year) and per-year read paths agree on the jittered freshness, because the
    /// jitter is keyed by territory rather than by territory and year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTtlJitterEnabled_ShouldKeepBatchAndPerYearPathsInAgreement()
    {
        TimeSpan effective = Bodu.Caching.CacheFreshness.WithJitter(JitterTtl, "US", 0.5);
        var clock = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildJitteredService(clock, 0.5, out CountingNotableDateService inner);

        // Warm two years through the multi-year (batch) path.
        _ = service.Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2027, 6, 1)), "US");
        int warmCount = inner.ResolveCount;

        // Move past the jittered expiry: both the single-year and the batch path must treat the years as stale.
        clock.Advance(effective + TimeSpan.FromMinutes(1));

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        Assert.IsTrue(inner.ResolveCount > warmCount, "the per-year path must observe the jittered staleness");

        int afterSingle = inner.ResolveCount;
        _ = service.Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2027, 6, 1)), "US");
        Assert.IsTrue(inner.ResolveCount > afterSingle, "the batch path must observe the same jittered staleness");
    }

    /// <summary>
    /// Verifies that the caller's territory casing does not change the effective time-to-live: the jitter key is the
    /// normalized territory.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTerritoryCasingDiffers_ShouldShareOneJitteredExpiry()
    {
        TimeSpan effective = Bodu.Caching.CacheFreshness.WithJitter(JitterTtl, "US", 0.5);
        var clock = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildJitteredService(clock, 0.5, out CountingNotableDateService inner);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "us");
        Assert.AreEqual(1, inner.ResolveCount);

        // Just inside the jittered expiry, the differently cased territory must still hit the same cached year.
        clock.Advance(effective - TimeSpan.FromMinutes(1));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        Assert.AreEqual(1, inner.ResolveCount, "casing must not change the effective time-to-live");

        clock.Advance(TimeSpan.FromMinutes(2));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "us");
        Assert.AreEqual(2, inner.ResolveCount, "both casings must expire at the same jittered instant");
    }
}

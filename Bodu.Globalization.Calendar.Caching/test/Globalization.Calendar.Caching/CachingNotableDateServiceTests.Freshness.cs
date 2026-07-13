// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Freshness.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the freshness triggers of <see cref="CachingNotableDateService" />: time-to-live expiry and resource-version
/// invalidation on reload.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that a cached year is recomputed once it is older than the time-to-live.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEntryOlderThanTtl_ShouldRecompute()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromHours(1), out CountingNotableDateService inner, out _);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        time.Advance(TimeSpan.FromHours(2));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(2, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that a cached year within the time-to-live is served without recomputing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEntryWithinTtl_ShouldServeFromCache()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromHours(1), out CountingNotableDateService inner, out _);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        time.Advance(TimeSpan.FromMinutes(30));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(1, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that reloading the observed resource invalidates the cache so the next query recomputes.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenResourceReloaded_ShouldRecompute()
    {
        var time = new MutableTimeProvider(Now);
        var provider = new MutableNotableDateResourceProvider(AmericasCalendarData.LoadResource("US"));
        var inner = new CountingNotableDateService();
        var cache = new InMemoryNotableDateCache();
        var service = new CachingNotableDateService(
            inner,
            cache,
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30) },
            provider,
            time);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        provider.Reload(AmericasCalendarData.LoadResource("US"));
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(2, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that without a reload the observed-resource version keeps serving from the cache.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenResourceUnchanged_ShouldServeFromCache()
    {
        var time = new MutableTimeProvider(Now);
        var provider = new MutableNotableDateResourceProvider(AmericasCalendarData.LoadResource("US"));
        var inner = new CountingNotableDateService();
        var service = new CachingNotableDateService(
            inner,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30) },
            provider,
            time);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(1, inner.ResolveCount);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Metrics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the hit, miss, and coalesced-flight counters the caching service emits through the
/// <c>Bodu.Globalization.Calendar.Caching</c> meter. Every test uses a territory unique to itself and filters on it,
/// because the meter is process-wide and tests run in parallel.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Builds a unique normalized territory tag for one test.
    /// </summary>
    /// <returns>A unique territory code.</returns>
    private static string UniqueTerritory() => $"ZZ-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    /// <summary>
    /// Verifies that a cold resolve then a warm resolve increment the miss and hit counters for the territory.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMissThenHit_ShouldIncrementMissAndHitCounters()
    {
        string territory = UniqueTerritory();
        using var collector = new TestMeterCollector(CalendarCachingMeter.MeterName);
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        _ = service.Resolve(new DateOnly(2026, 1, 1), territory);
        _ = service.Resolve(new DateOnly(2026, 1, 1), territory);

        Assert.AreEqual(1, collector.Sum("bodu.calendar.notable_date_cache.misses", "territory", territory), "the cold resolve must count one miss");
        Assert.AreEqual(1, collector.Sum("bodu.calendar.notable_date_cache.hits", "territory", territory), "the warm resolve must count one hit");
    }

    /// <summary>
    /// Verifies that concurrent cold callers joining one in-flight computation increment the coalesced-flight counter.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenConcurrentColdCallersCoalesce_ShouldIncrementCoalescedFlightCounter()
    {
        string territory = UniqueTerritory();
        using var collector = new TestMeterCollector(CalendarCachingMeter.MeterName);
        var gated = new GatedNotableDateService();
        var service = new CachingNotableDateService(
            gated,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        const int callers = 6;
        var started = new CountdownEvent(callers);
        Task[] tasks = [.. Enumerable.Range(0, callers).Select(i => Task.Run(() =>
        {
            started.Signal();
            started.Wait(TimeSpan.FromSeconds(30));
            _ = service.Resolve(new DateOnly(2026, 1, 1), territory);
        }))];

        SpinWait.SpinUntil(() => gated.ResolveCount >= 1, TimeSpan.FromSeconds(30));
        gated.Open();
        Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

        Assert.AreEqual(1, gated.ResolveCount, "sanity: the callers must have coalesced onto one computation");
        Assert.IsTrue(
            collector.Sum("bodu.calendar.notable_date_cache.coalesced_flights", "territory", territory) >= 1,
            "at least one joiner must be counted as coalesced");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that concurrent cold resolutions for the same territory and year coalesce into a single inner
    /// computation: every caller receives the shared result and the inner service runs exactly once.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenConcurrentColdCallsForSameYear_ShouldComputeInnerExactlyOnce()
    {
        var gated = new GatedNotableDateService();
        var service = new CachingNotableDateService(
            gated,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        const int callers = 8;
        var results = new IReadOnlyList<NotableDate>[callers];
        var started = new CountdownEvent(callers);
        Task[] tasks = [.. Enumerable.Range(0, callers).Select(i => Task.Run(() =>
        {
            started.Signal();
            started.Wait(TimeSpan.FromSeconds(30));
            results[i] = service.Resolve(new DateOnly(2026, 1, 1), "US");
        }))];

        // Let the pile-up form on the gate, then release the single in-flight computation.
        SpinWait.SpinUntil(() => gated.ResolveCount >= 1, TimeSpan.FromSeconds(30));
        gated.Open();
        Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

        Assert.AreEqual(1, gated.ResolveCount, "concurrent cold callers must share one inner computation");
        foreach (IReadOnlyList<NotableDate> result in results)
        {
            Assert.IsNotNull(result);
            Assert.HasCount(1, result);
        }
    }

    /// <summary>
    /// Verifies that a faulted computation does not poison the coalescing key: the failing call surfaces its
    /// exception, and the next call for the same year recomputes and succeeds.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFirstComputationThrows_ShouldRecomputeOnNextCall()
    {
        var gated = new GatedNotableDateService(throwOnFirstCall: true);
        gated.Open();
        var service = new CachingNotableDateService(
            gated,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        });

        IReadOnlyList<NotableDate> occurrences = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(2, gated.ResolveCount, "the failed flight must be evicted so the next call recomputes");
        Assert.HasCount(1, occurrences);
    }

    /// <summary>
    /// Verifies that a caller which observed a miss before the winning computation stored its entry, but reached the
    /// coalescing dictionary after that flight was retired, is served the freshly cached year instead of recomputing
    /// it — the inner service still runs exactly once.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLateJoinerMissesTheCompletedFlight_ShouldServeCachedYearWithoutRecomputing()
    {
        var inner = new CountingNotableDateService();
        using var cache = new LateJoinerNotableDateCache(new InMemoryNotableDateCache());
        var service = new CachingNotableDateService(
            inner,
            cache,
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        var date = new DateOnly(2026, 1, 1);

        // The late joiner parks inside its lookup holding the miss it just observed.
        Task<IReadOnlyList<NotableDate>> lateJoiner = Task.Run(() => service.Resolve(date, "US"));
        Assert.IsTrue(cache.WaitUntilParked(TimeSpan.FromSeconds(30)), "the late joiner never parked");

        // The winner computes, stores, and retires its flight while the late joiner is still parked.
        IReadOnlyList<NotableDate> winner = service.Resolve(date, "US");

        // Released, the late joiner now finds no flight to join even though the year is cached.
        cache.Release();
        Assert.IsTrue(lateJoiner.Wait(TimeSpan.FromSeconds(30)), "the late joiner never completed");

        Assert.AreEqual(1, inner.ResolveCount, "a late joiner must serve the cached year rather than recompute it");
        Assert.HasCount(1, winner);
        Assert.HasCount(1, lateJoiner.Result);
    }
}

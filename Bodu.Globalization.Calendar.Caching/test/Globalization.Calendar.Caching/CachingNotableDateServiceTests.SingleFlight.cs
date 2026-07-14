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
}

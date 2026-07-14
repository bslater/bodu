// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.BatchRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the multi-year batch read of <see cref="CachingNotableDateService" />: a batch-capable backend is read once
/// per range, a per-year-only backend is served through the fallback, and both produce identical results.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that a fully cached multi-year range against a batch-capable backend performs exactly one batch read and
    /// no per-year reads.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiYearRangeCachedAndBackendSupportsBatch_ShouldReadOnce()
    {
        var time = new MutableTimeProvider(Now);
        var counting = new CountingNotableDateCache(supportsBatch: true);
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31));
        var service = new CachingNotableDateService(
            new CountingNotableDateService(),
            counting.AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: time);

        _ = service.Resolve(range, "US");
        int batchesAfterWarmup = counting.GetYearsCount;
        IReadOnlyList<NotableDate> occurrences = service.Resolve(range, "US");

        Assert.AreEqual(3, occurrences.Count);
        Assert.AreEqual(batchesAfterWarmup + 1, counting.GetYearsCount);
        Assert.AreEqual(0, counting.GetYearCount);
    }

    /// <summary>
    /// Verifies that a multi-year range against a per-year-only backend is served through the per-year fallback and
    /// yields the same occurrences as the batch path.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiYearRangeAndBackendLacksBatch_ShouldFallBackToPerYearReads()
    {
        var time = new MutableTimeProvider(Now);
        var counting = new CountingNotableDateCache(supportsBatch: false);
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31));
        var service = new CachingNotableDateService(
            new CountingNotableDateService(),
            counting.AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: time);

        _ = service.Resolve(range, "US");
        int perYearAfterWarmup = counting.GetYearCount;
        IReadOnlyList<NotableDate> occurrences = service.Resolve(range, "US");

        Assert.AreEqual(3, occurrences.Count);
        Assert.AreEqual(perYearAfterWarmup + 3, counting.GetYearCount);
        Assert.AreEqual(0, counting.GetYearsCount);
    }

    /// <summary>
    /// Verifies that a partially cached multi-year batch read recomputes only the missing years.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBatchSpanPartiallyCached_ShouldRecomputeOnlyMissingYears()
    {
        var time = new MutableTimeProvider(Now);
        var counting = new CountingNotableDateCache(supportsBatch: true);
        var inner = new CountingNotableDateService();
        var service = new CachingNotableDateService(
            inner,
            counting.AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: time);

        // Warm only 2027 through a single-year query, then span 2026-2028.
        _ = service.Resolve(new DateRange(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)), "US");
        int computedAfterWarmup = inner.ResolveCount;

        IReadOnlyList<NotableDate> occurrences = service.Resolve(
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31)),
            "US");

        Assert.AreEqual(3, occurrences.Count);
        Assert.AreEqual(computedAfterWarmup + 2, inner.ResolveCount);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Ordering.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the ordering behaviour of <see cref="CachingNotableDateService" />: the zero-copy whole-year fast path,
/// order preservation across assembled ranges, and the sort fallback for a cache backend that violates the ordering
/// contract.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that a whole-civil-year query served from the cache returns the cached occurrence list itself, without
    /// copying or re-sorting.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWholeYearServedFromCache_ShouldReturnCachedListWithoutCopying()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out _, out InMemoryNotableDateCache cache);
        var year = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        _ = service.Resolve(year, "US");
        IReadOnlyList<NotableDate> served = service.Resolve(year, "US");

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "fixed", TimeSpan.FromDays(30), Now);
        Assert.IsNotNull(entry);
        Assert.IsTrue(ReferenceEquals(entry.Occurrences, served));
    }

    /// <summary>
    /// Verifies that a multi-year range assembled from cached years is ordered by emitted date then identity.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiYearRange_ShouldReturnOccurrencesOrderedByDate()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out _, out _);
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31));

        _ = service.Resolve(range, "US");
        IReadOnlyList<NotableDate> occurrences = service.Resolve(range, "US");

        for (int i = 1; i < occurrences.Count; i++)
            Assert.IsTrue(occurrences[i - 1].Date <= occurrences[i].Date);
    }

    /// <summary>
    /// Verifies that a cache backend violating the ordering contract still yields date-ordered output through the sort
    /// fallback.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBackendViolatesOrderingContract_ShouldSortFallback()
    {
        var time = new MutableTimeProvider(Now);
        var service = new CachingNotableDateService(
            new CountingNotableDateService(),
            new OrderViolatingNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: time);

        IReadOnlyList<NotableDate> occurrences = service.Resolve(
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            "US");

        Assert.AreEqual(2, occurrences.Count);
        Assert.AreEqual(new DateOnly(2026, 1, 1), occurrences[0].Date);
        Assert.AreEqual(new DateOnly(2026, 7, 5), occurrences[1].Date);
    }
}

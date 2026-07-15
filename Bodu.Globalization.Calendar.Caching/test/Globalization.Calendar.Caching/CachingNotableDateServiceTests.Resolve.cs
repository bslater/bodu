// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the resolution behaviour of <see cref="CachingNotableDateService" />: cache hits, range assembly, window
/// clipping, filter post-application, and cache population.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that resolving the same year twice computes it once and serves the second call from the cache.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameYearResolvedTwice_ShouldComputeOnce()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(1, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that resolving a single day returns only the occurrences emitted on that day.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDay_ShouldClipToThatDay()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out _, out _);

        IReadOnlyList<NotableDate> onNewYear = service.Resolve(new DateOnly(2026, 1, 1), "US");
        IReadOnlyList<NotableDate> onSecond = service.Resolve(new DateOnly(2026, 1, 2), "US");

        Assert.AreEqual(1, onNewYear.Count);
        Assert.AreEqual(0, onSecond.Count);
    }

    /// <summary>
    /// Verifies that a multi-year range computes each spanned year once and assembles the occurrences across them.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiYearRange_ShouldAssembleFromPerYearEntries()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        IReadOnlyList<NotableDate> occurrences = service.Resolve(
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31)),
            "US");

        Assert.AreEqual(3, occurrences.Count);
        Assert.AreEqual(3, inner.ResolveCount);
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1) },
            occurrences.Select(o => o.Date).ToArray());
    }

    /// <summary>
    /// Verifies that the filtered overload applies the filter after resolving the unfiltered result.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFilterExcludesAll_ShouldReturnEmptyWithoutBypassingCache()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);
        NotableDateFilter observanceOnly = NotableDateFilter.ForCategory(NotableDateCategory.Observance);

        IReadOnlyList<NotableDate> filtered = service.Resolve(new DateOnly(2026, 1, 1), "US", observanceOnly);
        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        Assert.AreEqual(0, filtered.Count);
        Assert.AreEqual(1, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that a resolved year is written to the cache stamped with the current instant.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMiss_ShouldPopulateCacheWithComputedInstant()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out _, out InMemoryNotableDateCache cache);

        _ = service.Resolve(new DateOnly(2026, 1, 1), "US");

        NotableDateCacheEntry? entry = cache.GetYear("US", 2026, "fixed", TimeSpan.FromDays(30), Now);
        Assert.IsNotNull(entry);
        Assert.AreEqual(Now, entry.ComputedAtUtc);
    }

    /// <summary>
    /// Verifies that an inverted range preserves the wrapped service's argument behaviour rather than returning empty.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRangeInverted_ShouldDelegateToInner()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        _ = service.Resolve(new DateRange(new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1)), "US");

        Assert.AreEqual(1, inner.ResolveCount);
    }

    /// <summary>
    /// Verifies that a null territory is rejected.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTerritoryNull_ShouldThrowArgumentNullException()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out _, out _);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.Resolve(new DateOnly(2026, 1, 1), null!);
        });
    }
}

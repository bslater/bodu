// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.ResolveFiltered.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>The whole-year 2026 range the filtered tests resolve.</summary>
    private static readonly DateRange Year2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    /// <summary>
    /// Verifies that a filter matching every occurrence returns the same content as the unfiltered resolution.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFilterMatchesEverything_ShouldReturnAllOccurrences()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        IReadOnlyList<NotableDate> unfiltered = service.Resolve(Year2026, "US");
        IReadOnlyList<NotableDate> filtered = service.Resolve(Year2026, "US", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

        CollectionAssert.AreEqual(unfiltered.ToList(), filtered.ToList());
    }

    /// <summary>
    /// Verifies that a partially matching filter returns exactly the matching subset, preserving order.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFilterMatchesSubset_ShouldReturnOnlyMatchesInOrder()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        IReadOnlyList<NotableDate> unfiltered = service.Resolve(Year2026, "US");
        NotableDateFilter firstOnly = NotableDateFilter.WithId(unfiltered[0].NotableDateId);
        IReadOnlyList<NotableDate> filtered = service.Resolve(Year2026, "US", firstOnly);

        Assert.IsTrue(filtered.Count >= 1);
        Assert.IsTrue(filtered.Count <= unfiltered.Count);
        foreach (NotableDate occurrence in filtered)
            Assert.AreEqual(unfiltered[0].NotableDateId, occurrence.NotableDateId);
    }

    /// <summary>
    /// Verifies that a filter matching nothing returns an empty list rather than <see langword="null" /> or a throw.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFilterMatchesNothing_ShouldReturnEmpty()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        IReadOnlyList<NotableDate> filtered = service.Resolve(Year2026, "US", NotableDateFilter.WithId("no-such-id"));

        Assert.IsNotNull(filtered);
        Assert.IsEmpty(filtered);
    }

    /// <summary>
    /// Verifies that filtering never affects what is cached: the full unfiltered year is stored, and a later
    /// unfiltered resolution serves it without recomputing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFiltered_ShouldCacheTheFullYear()
    {
        CachingNotableDateService service = BuildService(
            new MutableTimeProvider(Now), TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        _ = service.Resolve(Year2026, "US", NotableDateFilter.WithId("no-such-id"));
        IReadOnlyList<NotableDate> unfiltered = service.Resolve(Year2026, "US");

        Assert.AreEqual(1, inner.ResolveCount, "the filtered call must have cached the full year");
        Assert.IsNotEmpty(unfiltered);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the resolution behavior of <see cref="CachingNotableDateService" />: cache hits, range assembly, window
/// clipping, filter post-application, and cache population.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// A fixture with a ten-day observance from 28 December, which runs into the next civil year, a five-day observance
    /// from 1 June, and a one-day holiday on 1 January.
    /// </summary>
    private const string MultiDayXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.multi-day">
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="summer-festival" displayName="Summer Festival" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="year-end-break" displayName="Year-End Break" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x" durationDays="10"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that a window cutting a multi-day occurrence - one that starts before the window, including one that
    /// starts in the previous civil year - resolves exactly as the wrapped service resolves it, through both the
    /// per-year and the batch read paths.
    /// </summary>
    /// <param name="start">The first day of the window, as an ISO 8601 date.</param>
    /// <param name="end">The last day of the window, as an ISO 8601 date.</param>
    /// <param name="supportsBatch">Whether the cache backend exposes the batch read seam.</param>
    [TestMethod]
    [DataRow("2027-01-02", "2027-01-03", true)]
    [DataRow("2027-01-06", "2027-01-06", false)]
    [DataRow("2026-06-03", "2026-06-04", true)]
    [DataRow("2026-12-01", "2027-01-31", true)]
    [DataRow("2026-12-01", "2027-01-31", false)]
    [DataRow("2026-12-30", "2027-01-02", true)]
    [DataRow("2027-01-03", "2028-12-31", false)]
    [DataRow("2025-06-01", "2027-12-31", true)]
    public void Resolve_WhenRangeCutsMultiDayOccurrence_ShouldMatchWrappedService(string start, string end, bool supportsBatch)
    {
        var range = new DateRange(
            DateOnly.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateOnly.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        var inner = new NotableDateService(NotableDateResourceLoader.Load(MultiDayXml));
        using var service = new CachingNotableDateService(
            inner,
            new CountingNotableDateCache(supportsBatch).AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        string[] expected = Describe(inner.Resolve(range, "US"));
        string[] computed = Describe(service.Resolve(range, "US"));
        string[] served = Describe(service.Resolve(range, "US"));

        CollectionAssert.AreEqual(expected, computed);
        CollectionAssert.AreEqual(expected, served);
    }

    /// <summary>
    /// Verifies that, for every same-day collision policy, a window cutting a multi-day occurrence that shares its first
    /// day with a higher-priority, higher-category holiday resolves exactly as the wrapped service resolves it, through
    /// both the per-year and the batch read paths.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    /// <param name="start">The first day of the window, as an ISO 8601 date.</param>
    /// <param name="end">The last day of the window, as an ISO 8601 date.</param>
    /// <param name="supportsBatch">Whether the cache backend exposes the batch read seam.</param>
    [TestMethod]
    [DataRow("KeepAll", "2026-12-29", "2026-12-30", true)]
    [DataRow("KeepAll", "2026-12-01", "2027-01-31", false)]
    [DataRow("HighestPriorityOnly", "2026-06-03", "2026-06-04", true)]
    [DataRow("HighestPriorityOnly", "2026-12-29", "2026-12-30", false)]
    [DataRow("HighestPriorityOnly", "2027-01-02", "2027-01-03", true)]
    [DataRow("HighestPriorityOnly", "2026-12-01", "2027-01-31", true)]
    [DataRow("HighestPriorityOnly", "2026-12-01", "2027-01-31", false)]
    [DataRow("CategoryPriority", "2026-06-03", "2026-06-04", false)]
    [DataRow("CategoryPriority", "2026-12-29", "2026-12-30", true)]
    [DataRow("CategoryPriority", "2027-01-02", "2027-01-03", false)]
    [DataRow("CategoryPriority", "2026-12-01", "2027-01-31", true)]
    [DataRow("Custom", "2026-06-03", "2026-06-04", true)]
    [DataRow("Custom", "2026-12-29", "2026-12-30", false)]
    [DataRow("Custom", "2027-01-02", "2027-01-03", true)]
    [DataRow("Custom", "2026-12-01", "2027-01-31", false)]
    public void Resolve_WhenRangeCutsCollidingMultiDayOccurrence_ShouldMatchWrappedServiceForEveryPolicy(string policy, string start, string end, bool supportsBatch)
    {
        var range = new DateRange(
            DateOnly.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateOnly.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        var inner = new NotableDateService(
            NotableDateResourceLoader.Load(CollidingMultiDayXml(policy)),
            new NotableDateServiceOptions { CollisionResolver = new KeepGreatestIdResolver() });
        using var service = new CachingNotableDateService(
            inner,
            new CountingNotableDateCache(supportsBatch).AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        string[] expected = Describe(inner.Resolve(range, "US"));
        string[] computed = Describe(service.Resolve(range, "US"));
        string[] served = Describe(service.Resolve(range, "US"));

        CollectionAssert.AreEqual(expected, computed);
        CollectionAssert.AreEqual(expected, served);
    }

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
    /// Verifies that an inverted range preserves the wrapped service's argument behavior rather than returning empty.
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

    /// <summary>
    /// Builds a fixture, parameterized by same-day collision policy, whose ten-day break from 28 December and five-day
    /// festival from 1 June each share their first day with a higher-priority, higher-category one-day holiday.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string CollidingMultiDayXml(string policy) => $"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.colliding-multi-day">
      <ResolutionPolicy sameDayCollisionPolicy="{policy}" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="summer-festival" displayName="Summer Festival" category="Cultural">
          <Rules><Rule id="x" priority="1" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="summer-holiday" displayName="Summer Holiday" category="PublicHoliday">
          <Rules><Rule id="x" priority="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="year-end-break" displayName="Year-End Break" category="Cultural">
          <Rules><Rule id="x" priority="1" durationDays="10"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="year-end-holiday" displayName="Year-End Holiday" category="PublicHoliday">
          <Rules><Rule id="x" priority="5"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Describes each occurrence by its emitted date, identity, and span, so two resolutions compare by value.
    /// </summary>
    /// <param name="occurrences">The occurrences to describe.</param>
    /// <returns>One description per occurrence, in order.</returns>
    private static string[] Describe(IReadOnlyList<NotableDate> occurrences) =>
        [.. occurrences.Select(o => string.Create(CultureInfo.InvariantCulture, $"{o.Date:yyyy-MM-dd} {o.NotableDateId}/{o.RuleId} x{o.DurationDays}"))];

    /// <summary>
    /// A custom collision resolver that keeps the occurrence with the ordinally greatest notable-date identifier.
    /// </summary>
    private sealed class KeepGreatestIdResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding) =>
            [colliding.OrderByDescending(n => n.NotableDateId, StringComparer.Ordinal).First()];
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.ResolutionPolicies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="CachingNotableDateService" /> serves what the wrapped service resolves when the resource's
/// observed-date range policy or span collision policy is not the default: a cached civil year is clipped to a window
/// by the policy's own inclusion test and assembled across years without losing or repeating an occurrence.
/// </summary>
public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// The windows the policy tests resolve, as pairs of ISO 8601 dates: single days either side of moved holidays,
    /// windows across a year end, whole civil years, and ranges of several years.
    /// </summary>
    private static readonly (string Start, string End)[] s_policyWindows =
    [
        ("2027-12-31", "2027-12-31"),
        ("2028-01-01", "2028-01-01"),
        ("2027-12-20", "2028-01-10"),
        ("2027-06-01", "2028-06-30"),
        ("2029-12-31", "2029-12-31"),
        ("2030-01-01", "2030-01-02"),
        ("2029-01-01", "2029-12-31"),
        ("2030-01-01", "2030-12-31"),
        ("2027-01-01", "2031-12-31"),
    ];

    /// <summary>
    /// Verifies that, with a resource whose actual dates control inclusion, a single-day query through the decorator
    /// includes a holiday moved back across the year end on its actual day, 1 January 2028, and not on the day it is
    /// emitted, 31 December 2027.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenActualDateControlsInclusion_ShouldServeTheHolidayOnItsActualDay()
    {
        using CachingNotableDateService service = PolicyService("ActualOccurrenceControlsInclusion", supportsBatch: true, out _);

        Assert.AreEqual(
            (0, "new-year"),
            (service.Resolve(new DateOnly(2027, 12, 31), "XX").Count(o => o.NotableDateId == "new-year"), service.Resolve(new DateOnly(2028, 1, 1), "XX").Single().NotableDateId));
    }

    /// <summary>
    /// Verifies that, for each observed-date range policy and through both the per-year and the batch read paths, every
    /// window resolves through the decorator, computed and then served from the cache, exactly as the wrapped service
    /// resolves it.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="supportsBatch">Whether the cache backend exposes the batch read seam.</param>
    [TestMethod]
    [DataRow("ActualOccurrenceControlsInclusion", true)]
    [DataRow("ActualOccurrenceControlsInclusion", false)]
    [DataRow("BothOccurrencesControlInclusion", true)]
    [DataRow("BothOccurrencesControlInclusion", false)]
    [DataRow("ObservedOccurrenceControlsInclusion", true)]
    public void Resolve_WhenObservedDateRangePolicyIsSet_ShouldMatchWrappedService(string policy, bool supportsBatch)
    {
        using CachingNotableDateService service = PolicyService(policy, supportsBatch, out INotableDateService inner);

        AssertMatchesWrappedService(service, inner);
    }

    /// <summary>
    /// Verifies that the decorator applies the policy of the resource it observes when it wraps a reloadable service:
    /// every window resolves as the wrapped service resolves it.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWrappingReloadableServiceWithActualPolicy_ShouldMatchWrappedService()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(MovedHolidaysXml("ActualOccurrenceControlsInclusion")));
        ReloadableNotableDateService inner = new(provider);
        using CachingNotableDateService service = new(
            inner,
            new CountingNotableDateCache(supportsBatch: true).AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30) },
            provider,
            new MutableTimeProvider(Now));

        AssertMatchesWrappedService(service, inner);
    }

    /// <summary>
    /// Verifies that, for each span collision policy, every window, including those that cut a chain settled by an
    /// occurrence in another civil year, resolves through the decorator as the wrapped service resolves it.
    /// </summary>
    /// <param name="policy">The span collision policy token.</param>
    [TestMethod]
    [DataRow("HighestPriorityOnly")]
    [DataRow("CategoryPriority")]
    [DataRow("Custom")]
    public void Resolve_WhenSpanCollisionPolicyIsSet_ShouldMatchWrappedService(string policy)
    {
        var inner = new NotableDateService(
            NotableDateResourceLoader.Load(SpanChainXml(policy)),
            new NotableDateServiceOptions { CollisionResolver = new KeepGreatestIdResolver() });
        using var service = new CachingNotableDateService(
            inner,
            new CountingNotableDateCache(supportsBatch: false).AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));

        AssertMatchesWrappedService(service, inner);
    }

    /// <summary>
    /// Builds a decorator over a service of <see cref="MovedHolidaysXml" /> for the supplied policy.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="supportsBatch">Whether the cache backend exposes the batch read seam.</param>
    /// <param name="inner">The wrapped service.</param>
    /// <returns>The caching service.</returns>
    private static CachingNotableDateService PolicyService(string policy, bool supportsBatch, out INotableDateService inner)
    {
        inner = new NotableDateService(NotableDateResourceLoader.Load(MovedHolidaysXml(policy)));
        return new CachingNotableDateService(
            inner,
            new CountingNotableDateCache(supportsBatch).AsCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: new MutableTimeProvider(Now));
    }

    /// <summary>
    /// Asserts that every window of <see cref="s_policyWindows" /> resolves through the decorator, when computed and when
    /// served, as the wrapped service resolves it.
    /// </summary>
    /// <param name="service">The caching service.</param>
    /// <param name="inner">The wrapped service.</param>
    private static void AssertMatchesWrappedService(CachingNotableDateService service, INotableDateService inner)
    {
        List<string> mismatches = new();
        foreach ((string start, string end) in s_policyWindows)
        {
            var range = new DateRange(
                DateOnly.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture));

            string[] expected = Describe(inner.Resolve(range, "XX"));
            string[] computed = Describe(service.Resolve(range, "XX"));
            string[] served = Describe(service.Resolve(range, "XX"));

            if (!expected.SequenceEqual(computed, StringComparer.Ordinal) || !expected.SequenceEqual(served, StringComparer.Ordinal))
                mismatches.Add(start + ".." + end);
        }

        Assert.AreEqual(0, mismatches.Count, string.Join(", ", mismatches));
    }

    /// <summary>
    /// Builds a resource with two holidays moved across a year end: New Year's Day moved back to Friday when it falls on
    /// a Saturday, as on 1 January 2028, and a year-end holiday moved forward to Tuesday when 31 December falls on a
    /// Monday, as in 2029; plus a two-day observance from 3 January that is never moved.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string MovedHolidaysXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.moved-holidays">
          <ResolutionPolicy observedDateRangePolicy="{policy}" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="friday-before">
              <Trigger type="IfDayOfWeek"><Weekday value="Saturday" /></Trigger>
              <Action type="AddDays" days="-1" />
              <Emission mode="ObservedOnly" reason="Moved back" />
            </AdjustmentPolicy>
            <AdjustmentPolicy id="tuesday-after">
              <Trigger type="IfDayOfWeek"><Weekday value="Monday" /></Trigger>
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Moved forward" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="new-year" displayName="New Year" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="January" day="1" /></Strategy>
                  <Adjustments><Adjustment policyRef="friday-before" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="year-end" displayName="Year End" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="December" day="31" /></Strategy>
                  <Adjustments><Adjustment policyRef="tuesday-after" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="early-january" displayName="Early January" category="Observance">
              <Rules><Rule id="x" durationDays="2"><Strategy><Fixed month="January" day="3" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource whose ten-day, priority-one break from 28 December runs into the next year and overlaps a
    /// priority-five holiday on 1 January and a priority-three observance on 4 January, for a span collision policy.
    /// </summary>
    /// <param name="policy">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string SpanChainXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.span-chain">
          <ResolutionPolicy spanCollisionPolicy="{policy}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="break" displayName="Break" category="Cultural">
              <Rules><Rule id="x" priority="1" durationDays="10"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="new-year" displayName="New Year" category="PublicHoliday">
              <Rules><Rule id="x" priority="5"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="observance" displayName="Observance" category="Observance">
              <Rules><Rule id="x" priority="3"><Strategy><Fixed month="January" day="4" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;
}

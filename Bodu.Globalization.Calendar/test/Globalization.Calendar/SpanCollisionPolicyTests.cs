// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanCollisionPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the resource's <see cref="ResolutionPolicy.SpanCollisionPolicy" />: after same-day settlement, every chain
/// of occurrences connected through overlapping emitted spans that holds a multi-day occurrence is settled as one group,
/// ranked as a same-day group is, whatever window is queried.
/// </summary>
[TestClass]
public sealed class SpanCollisionPolicyTests
{
    /// <summary>The window holding the June overlap of <see cref="OverlapXml" />.</summary>
    private static readonly DateRange June = new(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

    /// <summary>
    /// Verifies that the default <see cref="CollisionPolicy.KeepAll" /> span policy keeps every overlapping occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenKeepAll_ShouldKeepEveryOverlappingOccurrence()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverlapXml("KeepAll", "KeepAll")));

        CollectionAssert.AreEqual(
            new[] { "2026-06-01 festival x5", "2026-06-03 holiday x1" },
            Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that the <see cref="CollisionPolicy.HighestPriorityOnly" /> span policy keeps the highest-priority
    /// occurrence of a multi-day span and the occurrence it overlaps, although they are emitted on different dates.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenHighestPriorityOnly_ShouldKeepTheHighestPriorityOccurrence()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverlapXml("KeepAll", "HighestPriorityOnly")));

        CollectionAssert.AreEqual(new[] { "2026-06-01 festival x5" }, Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that the <see cref="CollisionPolicy.CategoryPriority" /> span policy keeps the occurrence of the
    /// highest-ranked category, here the lower-priority public holiday inside the cultural festival.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCategoryPriority_ShouldKeepTheHighestRankedCategory()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverlapXml("KeepAll", "CategoryPriority")));

        CollectionAssert.AreEqual(new[] { "2026-06-03 holiday x1" }, Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that the <see cref="CollisionPolicy.Custom" /> span policy defers to the service's collision resolver,
    /// passing the group's earliest emitted date and its occurrences in resolution order.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCustom_ShouldPassTheGroupAndItsEarliestDateToTheResolver()
    {
        RecordingResolver resolver = new();
        NotableDateService service = new(
            NotableDateResourceLoader.Load(OverlapXml("KeepAll", "Custom")),
            new NotableDateServiceOptions { CollisionResolver = resolver });

        IReadOnlyList<NotableDate> result = service.Resolve(June, "XX");

        CollectionAssert.AreEqual(new[] { "2026-06-03 holiday x1" }, Describe(result));
        CollectionAssert.AreEqual(new[] { "2026-06-01: festival, holiday" }, resolver.Calls);
    }

    /// <summary>
    /// Verifies that the <see cref="CollisionPolicy.Custom" /> span policy keeps every occurrence of a group when the
    /// service has no collision resolver, as the same-day policy does.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCustomHasNoResolver_ShouldKeepEveryOccurrence()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverlapXml("KeepAll", "Custom")));

        CollectionAssert.AreEqual(
            new[] { "2026-06-01 festival x5", "2026-06-03 holiday x1" },
            Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that a group is the whole chain of occurrences connected through overlap, not each overlapping pair: the
    /// highest-priority occurrence of a chain whose first and last members do not overlap removes both, while an
    /// occurrence outside the chain is untouched.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOverlapsFormAChain_ShouldSettleTheWholeChainAsOneGroup()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(ChainXml("HighestPriorityOnly")));

        IReadOnlyList<NotableDate> march = service.Resolve(new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), "XX");

        CollectionAssert.AreEqual(new[] { "2026-03-05 third x1", "2026-03-10 apart x1" }, Describe(march));
    }

    /// <summary>
    /// Verifies that the span policy applies after same-day settlement: the multi-day occurrence that loses its emitted
    /// date takes no part in the span groups, so the occurrence it overlapped survives.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameDayPolicySettles_ShouldApplyTheSpanPolicyAfterwards()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OrderingXml("HighestPriorityOnly", "HighestPriorityOnly")));

        CollectionAssert.AreEqual(
            new[] { "2026-06-01 fair x1", "2026-06-03 holiday x1" },
            Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that, with a same-day policy that keeps every occurrence, the span policy settles the multi-day
    /// occurrence together with both occurrences it overlaps.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameDayPolicyKeepsAll_ShouldSettleTheWholeChain()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OrderingXml("KeepAll", "HighestPriorityOnly")));

        CollectionAssert.AreEqual(new[] { "2026-06-01 fair x1" }, Describe(service.Resolve(June, "XX")));
    }

    /// <summary>
    /// Verifies that one collision resolver serves both policies, consulted first for the same-day group and then for
    /// the span group that the same-day survivor forms, each given its group's date.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBothPoliciesAreCustom_ShouldConsultTheResolverForEachGroupInOrder()
    {
        RecordingResolver resolver = new();
        NotableDateService service = new(
            NotableDateResourceLoader.Load(OrderingXml("Custom", "Custom")),
            new NotableDateServiceOptions { CollisionResolver = resolver });

        IReadOnlyList<NotableDate> result = service.Resolve(June, "XX");

        CollectionAssert.AreEqual(new[] { "2026-06-03 holiday x1" }, Describe(result));
        CollectionAssert.AreEqual(new[] { "2026-06-01: fair, festival", "2026-06-01: festival, holiday" }, resolver.Calls);
    }

    /// <summary>
    /// Verifies that occurrences that overlap only by sharing an emitted date, none of them multi-day, are left to the
    /// same-day policy: a span policy that would reduce them keeps both.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOverlapHoldsNoMultiDayOccurrence_ShouldLeaveItToTheSameDayPolicy()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(SameDateSingleDaysXml("HighestPriorityOnly")));

        CollectionAssert.AreEqual(
            new[] { "2026-07-04 high x1", "2026-07-04 low x1" },
            Describe(service.Resolve(new DateOnly(2026, 7, 4), "XX")));
    }

    /// <summary>
    /// Verifies that, for every span policy, the result for each window of a sweep across two civil years equals a wide
    /// resolution restricted to the window, including windows that cut a chain settled by an occurrence outside the
    /// window and chains that run into the next civil year.
    /// </summary>
    /// <param name="sameDay">The same-day collision policy token.</param>
    /// <param name="span">The span collision policy token.</param>
    [TestMethod]
    [DataRow("KeepAll", "HighestPriorityOnly")]
    [DataRow("KeepAll", "CategoryPriority")]
    [DataRow("KeepAll", "Custom")]
    [DataRow("HighestPriorityOnly", "HighestPriorityOnly")]
    [DataRow("CategoryPriority", "Custom")]
    public void Resolve_WhenWindowSwept_ShouldEqualWideResolutionRestrictedToWindow(string sameDay, string span)
    {
        NotableDateService service = new(
            NotableDateResourceLoader.Load(SweepXml(sameDay, span)),
            new NotableDateServiceOptions { CollisionResolver = new KeepGreatestIdResolver() });
        IReadOnlyList<NotableDate> wide = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2027, 12, 31)), "XX");
        List<string> mismatches = new();

        for (DateOnly start = new(2025, 12, 15); start <= new DateOnly(2027, 1, 15); start = start.AddDays(1))
        {
            foreach (int length in new[] { 1, 3, 12 })
            {
                DateRange range = new(start, start.AddDays(length - 1));
                string[] expected = Describe(wide.Where(o => o.Date <= range.EndDate && o.EndDate >= range.StartDate).ToList());
                string[] actual = Describe(service.Resolve(range, "XX"));

                if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                    mismatches.Add(string.Create(CultureInfo.InvariantCulture, $"{range.StartDate:yyyy-MM-dd}..{range.EndDate:yyyy-MM-dd}"));
            }
        }

        Assert.AreEqual(0, mismatches.Count, string.Join(", ", mismatches.Take(10)));
    }

    /// <summary>
    /// Verifies that a chain that never ends, a weekly eight-day occurrence, is settled over the civil years a query
    /// widens to while the chain crosses the gathered boundary: an occurrence up to ten years beyond the one-year margin
    /// on either side, in 2015 or 2037 for a query in 2026, takes part, so the weekly occurrences in the window lose to it.
    /// </summary>
    /// <param name="peakYear">The one year in which the high-priority occurrence is emitted.</param>
    [TestMethod]
    [DataRow(2015)]
    [DataRow(2027)]
    [DataRow(2037)]
    public void Resolve_WhenNeverEndingChainReachesAnOccurrenceWithinTheWidening_ShouldSettleTheChainWithIt(int peakYear)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(NeverEndingChainXml(peakYear)));

        IReadOnlyList<NotableDate> window = service.Resolve(new DateRange(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8)), "XX");

        CollectionAssert.AreEqual(Array.Empty<string>(), Describe(window));
    }

    /// <summary>
    /// Verifies that a chain that never ends is settled over what was gathered once the widening reaches ten civil years
    /// beyond the one-year margin: an occurrence in 2014 or 2038, beyond that limit for a query in 2026, takes no part,
    /// so the weekly occurrences in the window are kept.
    /// </summary>
    /// <param name="peakYear">The one year in which the high-priority occurrence is emitted.</param>
    [TestMethod]
    [DataRow(2014)]
    [DataRow(2038)]
    public void Resolve_WhenNeverEndingChainReachesAnOccurrenceBeyondTheWidening_ShouldSettleTheChainWithoutIt(int peakYear)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(NeverEndingChainXml(peakYear)));

        IReadOnlyList<NotableDate> window = service.Resolve(new DateRange(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8)), "XX");

        CollectionAssert.AreEqual(new[] { "2026-02-23 rolling x8", "2026-03-02 rolling x8" }, Describe(window));
    }

    /// <summary>
    /// Verifies that a single-day query applies the span policy as the range that holds it does: a day inside the
    /// festival's span returns only the survivor.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDayInsideSettledSpan_ShouldReturnOnlyTheSurvivor()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverlapXml("KeepAll", "CategoryPriority")));

        CollectionAssert.AreEqual(Array.Empty<string>(), Describe(service.Resolve(new DateOnly(2026, 6, 2), "XX")));
        CollectionAssert.AreEqual(new[] { "2026-06-03 holiday x1" }, Describe(service.Resolve(new DateOnly(2026, 6, 3), "XX")));
    }

    /// <summary>
    /// Builds a resource whose five-day, priority-nine cultural festival from 1 June overlaps a priority-five public
    /// holiday on 3 June.
    /// </summary>
    /// <param name="sameDay">The same-day collision policy token.</param>
    /// <param name="span">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string OverlapXml(string sameDay, string span) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-overlap">
          <ResolutionPolicy sameDayCollisionPolicy="{sameDay}" spanCollisionPolicy="{span}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="festival" displayName="Festival" category="Cultural">
              <Rules><Rule id="x" priority="9" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday">
              <Rules><Rule id="x" priority="5"><Strategy><Fixed month="June" day="3" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource whose three occurrences form a chain, 1-3 March (priority one), 3-5 March (priority five) and 5
    /// March (priority nine), the first and last not overlapping, and whose fourth, on 10 March, overlaps none of them.
    /// </summary>
    /// <param name="span">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string ChainXml(string span) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-chain">
          <ResolutionPolicy spanCollisionPolicy="{span}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="first" displayName="First" category="Observance">
              <Rules><Rule id="x" priority="1" durationDays="3"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="second" displayName="Second" category="Observance">
              <Rules><Rule id="x" priority="5" durationDays="3"><Strategy><Fixed month="March" day="3" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="third" displayName="Third" category="Observance">
              <Rules><Rule id="x" priority="9"><Strategy><Fixed month="March" day="5" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="apart" displayName="Apart" category="Observance">
              <Rules><Rule id="x" priority="0"><Strategy><Fixed month="March" day="10" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource whose five-day, priority-one festival from 1 June shares its date with a priority-nine fair and
    /// overlaps a priority-five holiday on 3 June.
    /// </summary>
    /// <param name="sameDay">The same-day collision policy token.</param>
    /// <param name="span">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string OrderingXml(string sameDay, string span) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-ordering">
          <ResolutionPolicy sameDayCollisionPolicy="{sameDay}" spanCollisionPolicy="{span}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="festival" displayName="Festival" category="Cultural">
              <Rules><Rule id="x" priority="1" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="fair" displayName="Fair" category="Observance">
              <Rules><Rule id="x" priority="9"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday">
              <Rules><Rule id="x" priority="5"><Strategy><Fixed month="June" day="3" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource with two single-day occurrences on 4 July of different priorities.
    /// </summary>
    /// <param name="span">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string SameDateSingleDaysXml(string span) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-single-days">
          <ResolutionPolicy sameDayCollisionPolicy="KeepAll" spanCollisionPolicy="{span}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="high" displayName="High" category="PublicHoliday">
              <Rules><Rule id="x" priority="9"><Strategy><Fixed month="July" day="4" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="low" displayName="Low" category="Observance">
              <Rules><Rule id="x" priority="1"><Strategy><Fixed month="July" day="4" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource of overlapping occurrences for window sweeps: a ten-day, priority-one break from 28 December
    /// that runs into the next year and overlaps a priority-five holiday on 1 January and a priority-three observance on
    /// 4 January, a three-day, priority-two event from 30 December that shares the break's chain, and a five-day,
    /// priority-four fair from 1 June that overlaps a priority-six observance on 4 June.
    /// </summary>
    /// <param name="sameDay">The same-day collision policy token.</param>
    /// <param name="span">The span collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string SweepXml(string sameDay, string span) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-sweep">
          <ResolutionPolicy sameDayCollisionPolicy="{sameDay}" spanCollisionPolicy="{span}" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="break" displayName="Break" category="Cultural">
              <Rules><Rule id="x" priority="1" durationDays="10"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="event" displayName="Event" category="Observance">
              <Rules><Rule id="x" priority="2" durationDays="3"><Strategy><Fixed month="December" day="30" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="new-year" displayName="New Year" category="PublicHoliday">
              <Rules><Rule id="x" priority="5"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="observance" displayName="Observance" category="Observance">
              <Rules><Rule id="x" priority="3"><Strategy><Fixed month="January" day="4" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="fair" displayName="Fair" category="Cultural">
              <Rules><Rule id="x" priority="4" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="june-day" displayName="June Day" category="Religious">
              <Rules><Rule id="x" priority="6"><Strategy><Fixed month="June" day="4" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a resource whose weekly Monday occurrence lasts eight days, so each overlaps the next and the chain never
    /// ends, and whose priority-nine occurrence on 15 June is emitted in one year only.
    /// </summary>
    /// <param name="peakYear">The one year in which the high-priority occurrence is emitted.</param>
    /// <returns>The resource XML.</returns>
    private static string NeverEndingChainXml(int peakYear) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.span-never-ending">
          <ResolutionPolicy spanCollisionPolicy="HighestPriorityOnly" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="rolling" displayName="Rolling" category="Observance">
              <Rules><Rule id="x" priority="1" durationDays="8"><Recurrence><Weekly><Day dayOfWeek="Monday" /></Weekly></Recurrence></Rule></Rules>
            </NotableDate>
            <NotableDate id="peak" displayName="Peak" category="Observance">
              <Rules>
                <Rule id="x" priority="9">
                  <Applicability fromYear="{peakYear}" toYear="{peakYear}" />
                  <Strategy><Fixed month="June" day="15" /></Strategy>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Describes each occurrence by its emitted date, concept and span, so two resolutions compare by value.
    /// </summary>
    /// <param name="occurrences">The occurrences to describe.</param>
    /// <returns>One description per occurrence, in order.</returns>
    private static string[] Describe(IReadOnlyList<NotableDate> occurrences) =>
        [.. occurrences.Select(o => string.Create(CultureInfo.InvariantCulture, $"{o.Date:yyyy-MM-dd} {o.NotableDateId} x{o.DurationDays}"))];

    /// <summary>
    /// A collision resolver that records each call, as its date and the colliding concepts, and keeps the occurrence
    /// with the ordinally greatest concept identifier.
    /// </summary>
    private sealed class RecordingResolver
        : INotableDateCollisionResolver
    {
        /// <summary>
        /// Gets the calls received, in order, each as the date and the concepts of the group.
        /// </summary>
        /// <value>The recorded calls.</value>
        public List<string> Calls { get; } = new();

        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)
        {
            Calls.Add(string.Create(CultureInfo.InvariantCulture, $"{date:yyyy-MM-dd}: {string.Join(", ", colliding.Select(n => n.NotableDateId))}"));

            return [colliding.OrderByDescending(n => n.NotableDateId, StringComparer.Ordinal).First()];
        }
    }

    /// <summary>
    /// A collision resolver that keeps the occurrence with the ordinally greatest concept identifier.
    /// </summary>
    private sealed class KeepGreatestIdResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding) =>
            [colliding.OrderByDescending(n => n.NotableDateId, StringComparer.Ordinal).First()];
    }
}

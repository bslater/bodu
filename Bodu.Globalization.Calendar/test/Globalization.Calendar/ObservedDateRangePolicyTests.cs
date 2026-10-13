// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDateRangePolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Extensions;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the resource's <see cref="ObservedDateRangePolicy" />: which span of an occurrence, the emitted one, the
/// actual one, or either, decides whether a query includes it, in range, by-year and single-day queries and the
/// working-day extensions built on them.
/// </summary>
[TestClass]
public sealed class ObservedDateRangePolicyTests
{
    /// <summary>The Monday, 4 May 2026, on which the holiday falls and is moved from.</summary>
    private static readonly DateOnly Actual = new(2026, 5, 4);

    /// <summary>The Tuesday, 5 May 2026, to which the holiday is moved.</summary>
    private static readonly DateOnly Observed = new(2026, 5, 5);

    /// <summary>
    /// Verifies that a single-day query includes the moved holiday on the days each policy names: the emitted day under
    /// the default, the actual day under <see cref="ObservedDateRangePolicy.ActualOccurrenceControlsInclusion" />, and
    /// both under <see cref="ObservedDateRangePolicy.BothOccurrencesControlInclusion" />.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="onActual">Whether a query for the actual day includes the holiday.</param>
    /// <param name="onObserved">Whether a query for the emitted day includes the holiday.</param>
    [TestMethod]
    [DataRow("ObservedOccurrenceControlsInclusion", false, true)]
    [DataRow("ActualOccurrenceControlsInclusion", true, false)]
    [DataRow("BothOccurrencesControlInclusion", true, true)]
    public void Resolve_WhenSingleDay_ShouldIncludeTheHolidayOnThePolicysDays(string policy, bool onActual, bool onObserved)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml(policy, durationDays: 1, mode: "ObservedOnly")));

        Assert.AreEqual(
            (onActual, onObserved),
            (service.Resolve(Actual, "XX").Any(), service.Resolve(Observed, "XX").Any()));
    }

    /// <summary>
    /// Verifies that the included occurrence is the emitted one, carrying its emitted date and its actual date, whichever
    /// span decided its inclusion.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenActualControlsInclusion_ShouldReturnTheEmittedOccurrence()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml("ActualOccurrenceControlsInclusion", durationDays: 1, mode: "ObservedOnly")));

        NotableDate holiday = service.Resolve(Actual, "XX").Single();

        Assert.AreEqual((Observed, (DateOnly?)Actual, true), (holiday.Date, holiday.ActualDate, holiday.IsObserved));
    }

    /// <summary>
    /// Verifies that the actual test uses the actual span, from the actual date through its duration: a two-day holiday
    /// moved from 4-5 May to 5-6 May is included on 5 May but not 6 May under the actual policy, and on 6 May but not 4
    /// May under the default.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="day">The queried day of May 2026.</param>
    /// <param name="included">Whether the query includes the holiday.</param>
    [TestMethod]
    [DataRow("ActualOccurrenceControlsInclusion", 4, true)]
    [DataRow("ActualOccurrenceControlsInclusion", 5, true)]
    [DataRow("ActualOccurrenceControlsInclusion", 6, false)]
    [DataRow("ObservedOccurrenceControlsInclusion", 4, false)]
    [DataRow("ObservedOccurrenceControlsInclusion", 6, true)]
    [DataRow("BothOccurrencesControlInclusion", 4, true)]
    [DataRow("BothOccurrencesControlInclusion", 6, true)]
    public void Resolve_WhenHolidaySpansDays_ShouldTestThePolicysSpan(string policy, int day, bool included)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml(policy, durationDays: 2, mode: "ObservedOnly")));

        Assert.AreEqual(included, service.Resolve(new DateOnly(2026, 5, day), "XX").Any());
    }

    /// <summary>
    /// Verifies that the actual and observed occurrences an <c>ActualAndObserved</c> policy emits, which share their
    /// actual date, enter and leave an actual-controlled query together.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenActualAndObservedAreBothEmitted_ShouldIncludeBothOnTheActualDayOnly()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml("ActualOccurrenceControlsInclusion", durationDays: 1, mode: "ActualAndObserved")));

        Assert.AreEqual(
            (2, 0),
            (service.Resolve(Actual, "XX").Count, service.Resolve(Observed, "XX").Count));
    }

    /// <summary>
    /// Verifies that the by-year query applies the policy: a holiday on Monday 31 December 2029 moved to 1 January 2030
    /// belongs to the year of its actual date under the actual policy, the year of its emitted date under the default,
    /// and both years under the either policy.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="in2029">Whether resolving 2029 includes the holiday.</param>
    /// <param name="in2030">Whether resolving 2030 includes the holiday.</param>
    [TestMethod]
    [DataRow("ObservedOccurrenceControlsInclusion", false, true)]
    [DataRow("ActualOccurrenceControlsInclusion", true, false)]
    [DataRow("BothOccurrencesControlInclusion", true, true)]
    public void Resolve_WhenByYear_ShouldIncludeTheHolidayInThePolicysYears(string policy, bool in2029, bool in2030)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(YearEndHolidayXml(policy)));
        DateOnly moved = new(2029, 12, 31);

        Assert.AreEqual(
            (in2029, in2030),
            (service.Resolve(2029, "XX").Any(o => o.ActualDate == moved), service.Resolve(2030, "XX").Any(o => o.ActualDate == moved)));
    }

    /// <summary>
    /// Verifies that collision groups stay keyed by the emitted date: a holiday moved onto a day held by a
    /// higher-priority holiday loses there, so an actual-controlled query for its actual day returns nothing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMovedHolidayLosesItsEmittedDate_ShouldBeAbsentFromItsActualDay()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml("ActualOccurrenceControlsInclusion", durationDays: 1, mode: "ObservedOnly", rival: true)));

        Assert.AreEqual(
            (0, "rival"),
            (service.Resolve(Actual, "XX").Count, service.Resolve(Observed, "XX").Single().NotableDateId));
    }

    /// <summary>
    /// Verifies that the working-day extensions follow the policy: under the actual policy the holiday makes its actual
    /// day non-working and leaves its emitted day a working day, the reverse of the default.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="actualNonWorking">Whether the actual day is non-working.</param>
    /// <param name="observedNonWorking">Whether the emitted day is non-working.</param>
    [TestMethod]
    [DataRow("ObservedOccurrenceControlsInclusion", false, true)]
    [DataRow("ActualOccurrenceControlsInclusion", true, false)]
    [DataRow("BothOccurrencesControlInclusion", true, true)]
    public void IsNonWorkingDay_WhenHolidayIsMoved_ShouldFollowThePolicy(string policy, bool actualNonWorking, bool observedNonWorking)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(MovedHolidayXml(policy, durationDays: 1, mode: "ObservedOnly")));

        Assert.AreEqual(
            (actualNonWorking, observedNonWorking),
            (Actual.IsNonWorkingDay(service, "XX"), Observed.IsNonWorkingDay(service, "XX")));
    }

    /// <summary>
    /// Verifies that, for each policy, the result for each window of a sweep equals a wide resolution restricted to the
    /// window by the policy's own test.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    [TestMethod]
    [DataRow("ActualOccurrenceControlsInclusion")]
    [DataRow("BothOccurrencesControlInclusion")]
    public void Resolve_WhenWindowSwept_ShouldEqualWideResolutionRestrictedByThePolicy(string policy)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(YearEndHolidayXml(policy)));
        IReadOnlyList<NotableDate> wide = service.Resolve(new DateRange(new DateOnly(2028, 1, 1), new DateOnly(2031, 12, 31)), "XX");
        List<string> mismatches = new();

        for (DateOnly start = new(2029, 12, 20); start <= new DateOnly(2030, 1, 10); start = start.AddDays(1))
        {
            foreach (int length in new[] { 1, 2, 5 })
            {
                DateRange range = new(start, start.AddDays(length - 1));
                string[] expected = Describe(wide.Where(o => Includes(policy, o, range)).ToList());
                string[] actual = Describe(service.Resolve(range, "XX"));

                if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                    mismatches.Add(string.Create(CultureInfo.InvariantCulture, $"{range.StartDate:yyyy-MM-dd}..{range.EndDate:yyyy-MM-dd}"));
            }
        }

        Assert.AreEqual(0, mismatches.Count, string.Join(", ", mismatches.Take(10)));
    }

    /// <summary>
    /// Applies a policy's inclusion test to an occurrence: the emitted span, the actual span, or either, meeting the
    /// window.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="range">The window.</param>
    /// <returns><see langword="true" /> when the policy includes the occurrence in the window.</returns>
    private static bool Includes(string policy, NotableDate occurrence, DateRange range)
    {
        DateOnly actualStart = occurrence.ActualDate ?? occurrence.Date;
        bool emitted = occurrence.Date <= range.EndDate && occurrence.EndDate >= range.StartDate;
        bool actual = actualStart <= range.EndDate && actualStart.AddDays(Math.Max(1, occurrence.DurationDays) - 1) >= range.StartDate;

        return policy switch
        {
            "ActualOccurrenceControlsInclusion" => actual,
            "BothOccurrencesControlInclusion" => actual || emitted,
            _ => emitted,
        };
    }

    /// <summary>
    /// Builds a resource whose non-working holiday on Monday 4 May is moved one day later by a Monday adjustment, and
    /// optionally a priority-nine rival on Tuesday 5 May that wins a same-day collision there.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <param name="durationDays">The holiday's duration in days.</param>
    /// <param name="mode">The adjustment's emission mode.</param>
    /// <param name="rival">Whether to declare the rival on 5 May.</param>
    /// <returns>The resource XML.</returns>
    private static string MovedHolidayXml(string policy, int durationDays, string mode, bool rival = false)
    {
        string rivalConcept = rival
            ? """
              <NotableDate id="rival" displayName="Rival" category="PublicHoliday" defaultNonWorkingDay="true">
                <Rules><Rule id="x" priority="9"><Strategy><Fixed month="May" day="5" /></Strategy></Rule></Rules>
              </NotableDate>
              """
            : string.Empty;

        return $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.observed-range">
              <ResolutionPolicy sameDayCollisionPolicy="HighestPriorityOnly" observedDateRangePolicy="{policy}" priorityDirection="HigherWins" />
              <AdjustmentPolicies>
                <AdjustmentPolicy id="next-day">
                  <Trigger type="IfDayOfWeek"><Weekday value="Monday" /></Trigger>
                  <Action type="AddDays" days="1" />
                  <Emission mode="{mode}" reason="Moved" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
                  <Rules>
                    <Rule id="x" priority="1" durationDays="{durationDays}">
                      <Strategy><Fixed month="May" day="4" /></Strategy>
                      <Adjustments><Adjustment policyRef="next-day" /></Adjustments>
                    </Rule>
                  </Rules>
                </NotableDate>
                {rivalConcept}
              </NotableDates>
            </NotableDateResource>
            """;
    }

    /// <summary>
    /// Builds a resource whose holiday on 31 December is moved one day later when it falls on a Monday, as in 2029, and
    /// whose single-day observance on 3 January is never moved.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string YearEndHolidayXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.observed-year-end">
          <ResolutionPolicy observedDateRangePolicy="{policy}" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="next-day">
              <Trigger type="IfDayOfWeek"><Weekday value="Monday" /></Trigger>
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Moved" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="year-end" displayName="Year End" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="December" day="31" /></Strategy>
                  <Adjustments><Adjustment policyRef="next-day" /></Adjustments>
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
    /// Describes each occurrence by its emitted date, actual date, concept and span, so two resolutions compare by value.
    /// </summary>
    /// <param name="occurrences">The occurrences to describe.</param>
    /// <returns>One description per occurrence, in order.</returns>
    private static string[] Describe(IReadOnlyList<NotableDate> occurrences) =>
        [.. occurrences.Select(o => string.Create(CultureInfo.InvariantCulture, $"{o.Date:yyyy-MM-dd} ({o.ActualDate:yyyy-MM-dd}) {o.NotableDateId} x{o.DurationDays}"))];
}

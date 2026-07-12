// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the recurrence strategies through the full resolution pipeline: that a rule authored with a
/// <c>&lt;Recurrence&gt;</c> source generates the expected base occurrences within a bounded window, in chronological
/// order, without duplicates, and independently of query-window size or culture.
/// </summary>
[TestClass]
public sealed class RecurrenceKnownAnswerTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a service over a single-concept document whose sole rule carries the supplied rule body and attributes.
    /// </summary>
    /// <param name="ruleBody">The rule child elements (a <c>&lt;Recurrence&gt;</c> and optional <c>&lt;Duration&gt;</c>).</param>
    /// <param name="ruleAttributes">Optional extra attributes for the rule element.</param>
    /// <returns>A service over the authored document.</returns>
    private static NotableDateService Service(string ruleBody, string ruleAttributes = "")
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.recurrence">
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Observance" defaultNonWorkingDay="false">
              <Rules>
                <Rule id="r" {ruleAttributes}>{ruleBody}</Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Resolves the occurrence start dates of concept <c>event</c> within a window.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="start">The inclusive window start.</param>
    /// <param name="end">The inclusive window end.</param>
    /// <returns>The ordered occurrence start dates.</returns>
    private static List<DateOnly> Occurrences(NotableDateService service, DateOnly start, DateOnly end) =>
        service.Resolve(new DateRange(start, end), Territory)
            .Where(r => r.NotableDateId == "event")
            .Select(r => r.Date)
            .OrderBy(d => d)
            .ToList();

    /// <summary>
    /// Verifies scenario A: every 14 days from 1 January 2026 yields the fortnightly grid within January.
    /// </summary>
    [TestMethod]
    public void DailyInterval_WhenFortnightly_ShouldYieldAlignedGrid()
    {
        NotableDateService service = Service("""<Recurrence><DailyInterval anchorDate="2026-01-01" intervalDays="14" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 29) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)));
    }

    /// <summary>
    /// Verifies scenario B: every Monday and Friday yields both weekday series merged in chronological order.
    /// </summary>
    [TestMethod]
    public void Weekly_WhenMondayAndFriday_ShouldMergeSeriesChronologically()
    {
        NotableDateService service = Service("""<Recurrence><Weekly intervalWeeks="1"><Day dayOfWeek="Friday" /><Day dayOfWeek="Monday" /></Weekly></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 16) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 18)));
    }

    /// <summary>
    /// Verifies scenario C: every second Tuesday phased from 6 January 2026 skips the off-phase Tuesdays.
    /// </summary>
    [TestMethod]
    public void Weekly_WhenBiweeklyTuesday_ShouldPhaseFromAnchor()
    {
        NotableDateService service = Service("""<Recurrence><Weekly intervalWeeks="2" anchorDate="2026-01-06"><Day dayOfWeek="Tuesday" /></Weekly></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 20), new DateOnly(2026, 2, 3), new DateOnly(2026, 2, 17) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28)));
    }

    /// <summary>
    /// Verifies scenario D: day 31 every month with <c>UseLastDayOfMonth</c> clamps only the short months, without drift.
    /// </summary>
    [TestMethod]
    public void MonthlyDay_WhenDay31UseLastDay_ShouldClampShortMonthsWithoutDrift()
    {
        NotableDateService service = Service("""<Recurrence><MonthlyDay dayOfMonth="31" intervalMonths="1" invalidDayBehavior="UseLastDayOfMonth" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 31) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31)));
    }

    /// <summary>
    /// Verifies that day 31 every month with <c>Skip</c> emits an occurrence only in months that contain the 31st.
    /// </summary>
    [TestMethod]
    public void MonthlyDay_WhenDay31Skip_ShouldSkipShortMonths()
    {
        NotableDateService service = Service("""<Recurrence><MonthlyDay dayOfMonth="31" intervalMonths="1" invalidDayBehavior="Skip" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 3, 31), new DateOnly(2026, 5, 31) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31)));
    }

    /// <summary>
    /// Verifies scenario E: the last Friday of every month resolves the final matching weekday each month.
    /// </summary>
    [TestMethod]
    public void MonthlyWeekday_WhenLastFriday_ShouldResolveFinalWeekday()
    {
        NotableDateService service = Service("""<Recurrence><MonthlyWeekday dayOfWeek="Friday" weekOrdinal="Last" intervalMonths="1" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 27), new DateOnly(2026, 3, 27), new DateOnly(2026, 4, 24) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));
    }

    /// <summary>
    /// Verifies that a fifth-weekday recurrence emits an occurrence only in months that contain a fifth match.
    /// </summary>
    [TestMethod]
    public void MonthlyWeekday_WhenFifthMonday_ShouldEmitOnlyWhereFifthExists()
    {
        NotableDateService service = Service("""<Recurrence><MonthlyWeekday dayOfWeek="Monday" weekOrdinal="Fifth" intervalMonths="1" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 3, 30) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));
    }

    /// <summary>
    /// Verifies scenario F: a recurrence with a fixed duration produces one span per generated base occurrence.
    /// </summary>
    [TestMethod]
    public void Weekly_WhenWithFixedDuration_ShouldSpanEachOccurrence()
    {
        NotableDateService service = Service("""<Recurrence><Weekly intervalWeeks="1"><Day dayOfWeek="Friday" /></Weekly></Recurrence>""", """durationDays="3" """);

        // The two Fridays in the window each carry a three-day span (Friday through Sunday).
        List<NotableDate> results = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15)), Territory)
            .Where(r => r.NotableDateId == "event")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual((new DateOnly(2026, 1, 2), 3, new DateOnly(2026, 1, 4)), (results[0].Date, results[0].DurationDays, results[0].EndDate));
        Assert.AreEqual((new DateOnly(2026, 1, 9), 3, new DateOnly(2026, 1, 11)), (results[1].Date, results[1].DurationDays, results[1].EndDate));
    }

    /// <summary>
    /// Verifies the query-window invariance invariant: resolving a wide range and filtering to a sub-window yields the
    /// same occurrences as resolving the sub-window directly.
    /// </summary>
    [TestMethod]
    public void Weekly_WhenResolvedOverDifferentWindows_ShouldBeQueryWindowInvariant()
    {
        NotableDateService service = Service("""<Recurrence><Weekly intervalWeeks="2" anchorDate="2026-01-06"><Day dayOfWeek="Tuesday" /></Weekly></Recurrence>""");

        List<DateOnly> wideFilteredToFebruary = Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.Month == 2 && d.Year == 2026)
            .ToList();
        List<DateOnly> februaryDirect = Occurrences(service, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        CollectionAssert.AreEqual(februaryDirect, wideFilteredToFebruary);
    }

    /// <summary>
    /// Verifies the inclusive-boundary invariant: an occurrence exactly on the range start is included, and the next
    /// occurrence after the range end is excluded.
    /// </summary>
    [TestMethod]
    public void DailyInterval_WhenRangeStartsOnOccurrence_ShouldIncludeStartAndExcludePast()
    {
        NotableDateService service = Service("""<Recurrence><DailyInterval anchorDate="2026-01-01" intervalDays="14" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 15) },
            Occurrences(service, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15)));
    }

    /// <summary>
    /// Verifies the cross-year continuity invariant: a fortnightly series preserves phase across 31 December / 1 January.
    /// </summary>
    [TestMethod]
    public void DailyInterval_WhenCrossingYearBoundary_ShouldPreservePhase()
    {
        NotableDateService service = Service("""<Recurrence><DailyInterval anchorDate="2025-12-31" intervalDays="2" /></Recurrence>""");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 4), new DateOnly(2026, 1, 6) },
            Occurrences(service, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7)));
    }
}

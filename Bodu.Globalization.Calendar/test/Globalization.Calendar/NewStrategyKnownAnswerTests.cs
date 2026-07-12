// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NewStrategyKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the seven new single-occurrence strategies (positional, ISO-week, dynamic-reference, and business-day)
/// through the full resolution pipeline, pinning each configuration to a confirmed Gregorian date.
/// </summary>
[TestClass]
public sealed class NewStrategyKnownAnswerTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a service over a document whose <c>&lt;NotableDates&gt;</c> body is supplied verbatim.
    /// </summary>
    /// <param name="notableDatesBody">The concepts to author.</param>
    /// <returns>A service over the authored document.</returns>
    private static NotableDateService Service(string notableDatesBody)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.strategy">
          <NotableDates>{notableDatesBody}</NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Authors a single concept whose sole rule carries the supplied strategy element.
    /// </summary>
    /// <param name="id">The concept id.</param>
    /// <param name="strategyXml">The strategy element.</param>
    /// <param name="nonWorking">Whether the concept is non-working.</param>
    /// <returns>The concept XML.</returns>
    private static string Concept(string id, string strategyXml, bool nonWorking = false) => $"""
        <NotableDate id="{id}" displayName="{id}" category="Other" defaultNonWorkingDay="{(nonWorking ? "true" : "false")}">
          <Rules><Rule id="r"><Strategy>{strategyXml}</Strategy></Rule></Rules>
        </NotableDate>
        """;

    /// <summary>
    /// Resolves the single occurrence of a concept in a year.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="id">The concept id.</param>
    /// <param name="year">The year to resolve.</param>
    /// <returns>The occurrence date, or <see langword="null" /> when there is none.</returns>
    private static DateOnly? Resolve(NotableDateService service, string id, int year) =>
        service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), Territory)
            .Where(r => r.NotableDateId == id)
            .Select(r => (DateOnly?)r.Date)
            .SingleOrDefault();

    /// <summary>
    /// Verifies the ordinal-day-of-month strategy for positive and negative ordinals, including leap February.
    /// </summary>
    /// <param name="month">The month.</param>
    /// <param name="ordinal">The signed ordinal.</param>
    /// <param name="year">The year.</param>
    /// <param name="expected">The expected date (ISO), or empty when no occurrence is expected.</param>
    [TestMethod]
    [DataRow(2, -1, 2026, "2026-02-28")]
    [DataRow(2, -1, 2028, "2028-02-29")]
    [DataRow(2, -2, 2028, "2028-02-28")]
    [DataRow(4, 15, 2026, "2026-04-15")]
    [DataRow(1, -31, 2026, "2026-01-01")]
    [DataRow(4, 31, 2026, "")]
    public void OrdinalDayOfMonth_ResolvesSignedOrdinal(int month, int ordinal, int year, string expected)
    {
        NotableDateService service = Service(Concept("ord", $"""<OrdinalDayOfMonth month="{month}" ordinal="{ordinal}" />"""));

        Assert.AreEqual(expected.Length == 0 ? null : DateOnly.Parse(expected), Resolve(service, "ord", year));
    }

    /// <summary>
    /// Verifies the day-of-year strategy for positive and negative ordinals, including leap years.
    /// </summary>
    /// <param name="ordinal">The signed ordinal.</param>
    /// <param name="year">The year.</param>
    /// <param name="expected">The expected date (ISO), or empty when no occurrence is expected.</param>
    [TestMethod]
    [DataRow(60, 2027, "2027-03-01")]
    [DataRow(60, 2028, "2028-02-29")]
    [DataRow(1, 2026, "2026-01-01")]
    [DataRow(-1, 2026, "2026-12-31")]
    [DataRow(366, 2028, "2028-12-31")]
    [DataRow(366, 2027, "")]
    public void DayOfYear_ResolvesSignedOrdinal(int ordinal, int year, string expected)
    {
        NotableDateService service = Service(Concept("doy", $"""<DayOfYear ordinal="{ordinal}" />"""));

        Assert.AreEqual(expected.Length == 0 ? null : DateOnly.Parse(expected), Resolve(service, "doy", year));
    }

    /// <summary>
    /// Verifies the ISO-week-date strategy, including week-year boundaries where the result falls in an adjacent
    /// Gregorian year.
    /// </summary>
    /// <param name="isoYear">The ISO week-year.</param>
    /// <param name="week">The ISO week.</param>
    /// <param name="dayOfWeek">The weekday.</param>
    /// <param name="expected">The expected Gregorian date (ISO).</param>
    [TestMethod]
    [DataRow(1, "Monday", "2027-01-04")]
    [DataRow(53, "Sunday", "2027-01-03")]
    [DataRow(1, "Monday", "2025-12-29")]
    public void IsoWeekDate_ResolvesWeekYear(int week, string dayOfWeek, string expected)
    {
        // The ISO week-year date can fall in an adjacent Gregorian year, so assert it via a single-day query on the
        // expected Gregorian date (the resolver evaluates each scan year's ISO week-year).
        NotableDateService service = Service(Concept("iso", $"""<IsoWeekDate week="{week}" dayOfWeek="{dayOfWeek}" />"""));
        DateOnly expectedDate = DateOnly.Parse(expected);

        Assert.AreEqual(
            expectedDate,
            service.Resolve(expectedDate, Territory).Where(r => r.NotableDateId == "iso").Select(r => (DateOnly?)r.Date).SingleOrDefault());
    }

    /// <summary>
    /// Verifies that a week-53 request for a 52-week ISO year produces no occurrence.
    /// </summary>
    [TestMethod]
    public void IsoWeekDate_WhenWeek53InShortYear_ShouldProduceNoOccurrence()
    {
        NotableDateService service = Service(Concept("iso", """<IsoWeekDate week="53" dayOfWeek="Monday" />"""));

        Assert.IsNull(Resolve(service, "iso", 2025));
    }

    /// <summary>
    /// Verifies the weekday-near-rule strategy: the first Monday after a fixed reference of 1 January 2026 (a Thursday).
    /// </summary>
    [TestMethod]
    public void WeekdayNearRule_WhenFirstMondayAfterReference_ShouldResolveDynamically()
    {
        NotableDateService service = Service(
            Concept("anchor", """<Fixed month="1" day="1" />""") +
            Concept("near", """<WeekdayNearRule notableDateRef="anchor" dayOfWeek="Monday" direction="After" />"""));

        Assert.AreEqual(new DateOnly(2026, 1, 5), Resolve(service, "near", 2026));
    }

    /// <summary>
    /// Verifies the nth-weekday-from-rule strategy: the second Monday after a reference of 1 January 2026 (a Thursday).
    /// </summary>
    [TestMethod]
    public void NthWeekdayFromRule_WhenSecondMondayAfter_ShouldCountMatches()
    {
        NotableDateService service = Service(
            Concept("anchor", """<Fixed month="1" day="1" />""") +
            Concept("nth", """<NthWeekdayFromRule notableDateRef="anchor" dayOfWeek="Monday" ordinal="2" />"""));

        Assert.AreEqual(new DateOnly(2026, 1, 12), Resolve(service, "nth", 2026));
    }

    /// <summary>
    /// Verifies the working-day-offset-from-rule strategy: the working day before Boxing Day 2026, where Christmas Day
    /// is also non-working, is 24 December 2026.
    /// </summary>
    [TestMethod]
    public void WorkingDayOffsetFromRule_WhenBeforeBoxingDay_ShouldSkipNonWorkingDays()
    {
        NotableDateService service = Service(
            Concept("christmas", """<Fixed month="12" day="25" />""", nonWorking: true) +
            Concept("boxing-day", """<Fixed month="12" day="26" />""", nonWorking: true) +
            Concept("last-working-day", """<WorkingDayOffsetFromRule notableDateRef="boxing-day" offsetWorkingDays="-1" />"""));

        Assert.AreEqual(new DateOnly(2026, 12, 24), Resolve(service, "last-working-day", 2026));
    }

    /// <summary>
    /// Verifies the working-day-in-month strategy: the first working day of January 2027, where New Year's Day (a
    /// Friday) is non-working, is Monday 4 January 2027.
    /// </summary>
    [TestMethod]
    public void WorkingDayInMonth_WhenFirstWorkingDayOfJanuary_ShouldSkipHolidayAndWeekend()
    {
        NotableDateService service = Service(
            Concept("new-year", """<Fixed month="1" day="1" />""", nonWorking: true) +
            Concept("first-working-day", """<WorkingDayInMonth month="1" ordinal="1" />"""));

        Assert.AreEqual(new DateOnly(2027, 1, 4), Resolve(service, "first-working-day", 2027));
    }

    /// <summary>
    /// Verifies the working-day-in-month strategy for a negative ordinal: the last working day of February 2026 is
    /// Friday 27 February 2026.
    /// </summary>
    [TestMethod]
    public void WorkingDayInMonth_WhenLastWorkingDayOfFebruary_ShouldCountFromEnd()
    {
        NotableDateService service = Service(Concept("last-working-day", """<WorkingDayInMonth month="2" ordinal="-1" />"""));

        Assert.AreEqual(new DateOnly(2026, 2, 27), Resolve(service, "last-working-day", 2026));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedDurationKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the calculated end-date duration feature through the full resolution pipeline: that a rule whose span is
/// bounded by a second date strategy resolves to the expected effective start, duration, and inclusive end date,
/// including cross-year spans and boundary inclusivity.
/// </summary>
[TestClass]
public sealed class CalculatedDurationKnownAnswerTests
{
    private const string Territory = "XX";

    /// <summary>
    /// The company year-end shutdown: the last working day is the Friday before Boxing Day (exclusive start), and staff
    /// return on the first Monday after New Year's Day (exclusive end).
    /// </summary>
    private const string ShutdownRule = """
      <Strategy><WeekdayNearDate month="12" day="26" dayOfWeek="Friday" direction="Before" /></Strategy>
      <Duration>
        <UntilDate startBoundary="Exclusive" endBoundary="Exclusive" selection="FirstOnOrAfterStart">
          <Strategy><WeekdayNearDate month="1" day="1" dayOfWeek="Monday" direction="After" /></Strategy>
        </UntilDate>
      </Duration>
    """;

    /// <summary>
    /// Builds a service over a single-concept document whose sole rule carries the supplied rule body.
    /// </summary>
    /// <param name="ruleBody">The rule child elements.</param>
    /// <returns>A service over the authored document.</returns>
    private static NotableDateService Service(string ruleBody)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.duration">
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Other" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="r">{ruleBody}</Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies the consolidated shutdown table: each start year resolves to the effective start, duration, and end from
    /// Section 13 of the requirements.
    /// </summary>
    /// <param name="startYear">The December start year.</param>
    /// <param name="expectedStart">The expected effective shutdown start (ISO date).</param>
    /// <param name="expectedDuration">The expected span length in days.</param>
    /// <param name="expectedEnd">The expected inclusive shutdown end (ISO date).</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "2024-12-21", 16, "2025-01-05")]
    [DataRow(2025, "2025-12-20", 16, "2026-01-04")]
    [DataRow(2026, "2026-12-26", 9, "2027-01-03")]
    [DataRow(2027, "2027-12-25", 9, "2028-01-02")]
    [DataRow(2028, "2028-12-23", 16, "2029-01-07")]
    public void CalculatedDuration_ForShutdown_ResolvesConsolidatedTable(int startYear, string expectedStart, int expectedDuration, string expectedEnd)
    {
        NotableDate shutdown = Service(ShutdownRule)
            .Resolve(new DateRange(new DateOnly(startYear, 12, 1), new DateOnly(startYear, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "event");

        Assert.AreEqual(
            (DateOnly.Parse(expectedStart), expectedDuration, DateOnly.Parse(expectedEnd)),
            (shutdown.Date, shutdown.DurationDays, shutdown.EndDate));
    }

    /// <summary>
    /// Verifies a same-year inclusive calculated range: 1 May through 5 May spans five days.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenSameYearInclusive_ShouldSpanInclusive()
    {
        NotableDate result = Service("""
          <Strategy><Fixed month="5" day="1" /></Strategy>
          <Duration><UntilDate startBoundary="Inclusive" endBoundary="Inclusive" selection="FirstOnOrAfterStart">
            <Strategy><Fixed month="5" day="5" /></Strategy></UntilDate></Duration>
        """).Resolve(new DateRange(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)), Territory).Single(r => r.NotableDateId == "event");

        Assert.AreEqual((new DateOnly(2026, 5, 1), 5, new DateOnly(2026, 5, 5)), (result.Date, result.DurationDays, result.EndDate));
    }

    /// <summary>
    /// Verifies a same-year exclusive calculated range: excluding both anchors narrows 1-5 May to 2-4 May.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenSameYearExclusive_ShouldExcludeBothAnchors()
    {
        NotableDate result = Service("""
          <Strategy><Fixed month="5" day="1" /></Strategy>
          <Duration><UntilDate startBoundary="Exclusive" endBoundary="Exclusive" selection="FirstOnOrAfterStart">
            <Strategy><Fixed month="5" day="5" /></Strategy></UntilDate></Duration>
        """).Resolve(new DateRange(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)), Territory).Single(r => r.NotableDateId == "event");

        Assert.AreEqual((new DateOnly(2026, 5, 2), 3, new DateOnly(2026, 5, 4)), (result.Date, result.DurationDays, result.EndDate));
    }

    /// <summary>
    /// Verifies that a same start and end anchor with exclusive boundaries yields a negative span and emits nothing,
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenSameAnchorExclusive_ShouldEmitNothing()
    {
        IReadOnlyList<NotableDate> results = Service("""
          <Strategy><Fixed month="5" day="1" /></Strategy>
          <Duration><UntilDate startBoundary="Exclusive" endBoundary="Exclusive" selection="FirstOnOrAfterStart">
            <Strategy><Fixed month="5" day="1" /></Strategy></UntilDate></Duration>
        """).Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Territory);

        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "event"));
    }

    /// <summary>
    /// Verifies that an end strategy earlier than the start rolls into the following year (20 December to 5 January).
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenEndRollsIntoNextYear_ShouldSelectFollowingYear()
    {
        NotableDate result = Service("""
          <Strategy><Fixed month="12" day="20" /></Strategy>
          <Duration><UntilDate startBoundary="Inclusive" endBoundary="Inclusive" selection="FirstOnOrAfterStart">
            <Strategy><Fixed month="1" day="5" /></Strategy></UntilDate></Duration>
        """).Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), Territory).Single(r => r.NotableDateId == "event");

        Assert.AreEqual((new DateOnly(2026, 12, 20), 17, new DateOnly(2027, 1, 5)), (result.Date, result.DurationDays, result.EndDate));
    }

    /// <summary>
    /// Verifies that a single-day query inside a cross-year shutdown span returns the whole occurrence unchanged.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenQueriedInsideCrossYearSpan_ShouldReturnWholeOccurrence()
    {
        NotableDate shutdown = Service(ShutdownRule)
            .Resolve(new DateOnly(2027, 1, 2), Territory)
            .Single(r => r.NotableDateId == "event");

        Assert.AreEqual((new DateOnly(2026, 12, 26), 9, new DateOnly(2027, 1, 3)), (shutdown.Date, shutdown.DurationDays, shutdown.EndDate));
    }

    /// <summary>
    /// Verifies that the exclusive start boundary day is not part of the shutdown span.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenQueryingExclusiveStartBoundary_ShouldNotReturnOccurrence()
    {
        IReadOnlyList<NotableDate> results = Service(ShutdownRule).Resolve(new DateOnly(2026, 12, 25), Territory);

        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "event"));
    }

    /// <summary>
    /// Verifies that the exclusive end boundary day is not part of the shutdown span.
    /// </summary>
    [TestMethod]
    public void CalculatedDuration_WhenQueryingExclusiveEndBoundary_ShouldNotReturnOccurrence()
    {
        IReadOnlyList<NotableDate> results = Service(ShutdownRule).Resolve(new DateOnly(2027, 1, 4), Territory);

        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "event"));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.OffsetFromRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class StrategyBoundaryGuardTests
{
    /// <summary>
    /// Verifies that an offset projecting past <see cref="DateOnly.MaxValue" /> yields no dependent occurrence without
    /// throwing, while the anchor still resolves.
    /// </summary>
    [TestMethod]
    public void OffsetFromRule_WhenProjectionOverflowsMaxValue_ShouldSkipWithoutThrowing()
    {
        // Single-day query on the year-end anchor: the year-9999 offset overflows (skipped), and the adjacent-year
        // fringe projection falls outside this day, so only the overflow path is in scope.
        IReadOnlyList<NotableDate> results = Build(OffsetForwardXml)
            .Resolve(new DateRange(new DateOnly(9999, 12, 31), new DateOnly(9999, 12, 31)), Territory);

        Assert.ContainsSingle(r => r.NotableDateId == "anchor", results, "anchor still resolves");
        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "dependent"), "overflowing dependent is skipped");
    }

    /// <summary>
    /// Verifies that an offset from a non-Gregorian anchor keeps every occurrence when the anchor's short lunar date
    /// lands twice in one Gregorian year, including the projection that crosses into the following Gregorian year
    /// (1 Ramadan fell on both 10 January and 30 December 1997 in the Umm al-Qura calendar, so the 26-day offset must
    /// produce 5 February 1997 and 25 January 1998).
    /// </summary>
    [TestMethod]
    public void OffsetFromRule_WhenAnchorOccursTwiceInYear_ShouldProjectBothOccurrences()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.offset-double-anchor">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="Observance">
              <Rules><Rule id="x"><Applicability calendar="UmmAlQura" /><Strategy><Fixed month="9" day="1" sweepCalendarYears="true" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="dependent" displayName="Dependent" category="Observance">
              <Rules><Rule id="x"><Strategy><OffsetFromRule notableDateRef="anchor" offsetDays="26" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        List<DateOnly> dependents = Build(xml)
            .Resolve(new DateRange(new DateOnly(1997, 1, 1), new DateOnly(1998, 12, 31)), Territory)
            .Where(r => r.NotableDateId == "dependent")
            .Select(r => r.Date)
            .OrderBy(d => d)
            .ToList();

        CollectionAssert.AreEqual(
            new List<DateOnly> { new(1997, 2, 5), new(1998, 1, 25) },
            dependents,
            $"resolved [{string.Join(", ", dependents)}]");
    }

    /// <summary>
    /// Verifies that an offset projecting before <see cref="DateOnly.MinValue" /> yields no dependent occurrence without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void OffsetFromRule_WhenProjectionUnderflowsMinValue_ShouldSkipWithoutThrowing()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.boundary-offset-back">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="Observance">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="dependent" displayName="Dependent" category="Observance">
              <Rules><Rule id="x"><Strategy><OffsetFromRule notableDateRef="anchor" offsetDays="-5" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = Build(xml)
            .Resolve(new DateRange(new DateOnly(1, 1, 1), new DateOnly(1, 1, 1)), Territory);

        Assert.ContainsSingle(r => r.NotableDateId == "anchor", results);
        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "dependent"));
    }

    /// <summary>
    /// Verifies that an in-range offset projection still resolves normally (regression guard against the overflow fix).
    /// </summary>
    [TestMethod]
    public void OffsetFromRule_WhenProjectionInRange_ShouldStillResolve()
    {
        NotableDate dependent = Build(OffsetForwardXml)
            .Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "dependent");

        Assert.AreEqual(new DateOnly(2027, 1, 5), dependent.Date);
    }
}

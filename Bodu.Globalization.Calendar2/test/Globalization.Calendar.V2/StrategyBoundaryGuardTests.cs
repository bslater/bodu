// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that the weekday-relative and offset strategies skip gracefully (yield no occurrence) when their
/// computation would roll past the representable date range at the year-1 and year-9999 extremes, rather than throwing
/// and failing a range query that spans the boundary.
/// </summary>
[TestClass]
public sealed class StrategyBoundaryGuardTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a service over an inline resource.
    /// </summary>
    /// <param name="xml">The resource XML.</param>
    /// <returns>A service over the resource.</returns>
    private static INotableDateService Build(string xml) =>
        new NotableDateService(NotableDateResourceLoader.Load(xml));

    /// <summary>
    /// A resource whose dependent rule offsets a year-end anchor forward, overflowing at year 9999.
    /// </summary>
    private const string OffsetForwardXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.boundary-offset-fwd">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="anchor" displayName="Anchor" category="Observance">
          <Rules><Rule id="x"><Strategy><Fixed month="December" day="31" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="dependent" displayName="Dependent" category="Observance">
          <Rules><Rule id="x"><Strategy><OffsetFromRule notableDateRef="anchor" offsetDays="5" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

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

        Assert.AreEqual(1, results.Count(r => r.NotableDateId == "anchor"), "anchor still resolves");
        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "dependent"), "overflowing dependent is skipped");
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

        Assert.AreEqual(1, results.Count(r => r.NotableDateId == "anchor"));
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

    /// <summary>
    /// Verifies that a relative-weekday rule whose seek rolls past year 9999 skips gracefully without throwing when a
    /// range query spans the boundary.
    /// </summary>
    [TestMethod]
    public void RelativeWeekdayInMonth_WhenSeekOverflowsAtYear9999_ShouldSkipWithoutThrowing()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.boundary-relative">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="r" displayName="R" category="Observance">
              <Rules><Rule id="x"><Strategy><RelativeWeekdayInMonth month="December" dayOfWeek="Friday" weekOrdinal="Last" relativeDayOfWeek="Saturday" direction="After" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        // The seek past the last Friday of December 9999 rolls into year 10000; resolution must not throw.
        IReadOnlyList<NotableDate> results = Build(xml)
            .Resolve(new DateRange(new DateOnly(9999, 1, 1), new DateOnly(9999, 12, 31)), Territory);

        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "r"));
    }
}

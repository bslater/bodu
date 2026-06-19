// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the weekday-relative and offset strategies skip gracefully (yield no occurrence) when their
/// computation would roll past the representable date range at the year-1 and year-9999 extremes, rather than throwing
/// and failing a range query that spans the boundary.
/// </summary>
[TestClass]
public sealed partial class StrategyBoundaryGuardTests
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

}

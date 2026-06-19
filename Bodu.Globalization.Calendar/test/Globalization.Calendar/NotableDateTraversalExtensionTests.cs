// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 enumeration and notable-date traversal matrices to the v2 explicit-service surface: enumerating
/// working, non-working and notable dates over one-week and single-day ranges, the next/previous non-working day
/// single step, and the next/previous notable date (earliest-after / latest-before, year roll, multi-day span advance,
/// and filter restriction). It also asserts <see cref="DateTime" /> and <see cref="DateTimeOffset" /> kind, offset and
/// time-of-day preservation on traversal.
/// </summary>
/// <remarks>
/// <para>
/// Reversed-range behaviour follows v2 semantics, which differ from v1: the ascending enumerators iterate
/// <c>start..end</c> and yield nothing when the bounds are reversed, and the date-range notable-date query throws
/// <see cref="ArgumentOutOfRangeException" /> for a reversed range rather than yielding an ascending sequence.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// A fixture with a single non-working public holiday on 1 January.
    /// </summary>
    private const string HolidayXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.traversal">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A fixture with several fixed observances used to exercise notable-date traversal and enumeration ordering.
    /// </summary>
    private const string CalendarXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.calendar">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="anzac-day" displayName="Anzac Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="April" day="25" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="festival" displayName="Festival" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="April" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="christmas-day" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A fixture whose only observance spans five days from 1 June, used to verify multi-day span traversal.
    /// </summary>
    private const string SpanXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.span">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="festival" displayName="Festival" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="August" day="15" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the single-holiday fixture.
    /// </summary>
    private static INotableDateService HolidayService =>
        new NotableDateService(NotableDateResourceLoader.Load(HolidayXml));

    /// <summary>
    /// Gets a service over the multi-observance calendar fixture.
    /// </summary>
    private static INotableDateService CalendarService =>
        new NotableDateService(NotableDateResourceLoader.Load(CalendarXml));

    /// <summary>
    /// Gets a service over the multi-day span fixture.
    /// </summary>
    private static INotableDateService SpanService =>
        new NotableDateService(NotableDateResourceLoader.Load(SpanXml));

    /// <summary>
    /// Provides single-day ranges in January 2026 paired with the number of working days each yields under the default
    /// working week (1 for a weekday, 0 for a weekend day).
    /// </summary>
    /// <returns>A sequence of <c>(int y, int m, int d, int expected)</c> rows.</returns>
    public static IEnumerable<object[]> SingleDayWorkingYieldRows()
    {
        yield return new object[] { 2026, 1, 5, 1 };   // Monday
        yield return new object[] { 2026, 1, 6, 1 };   // Tuesday
        yield return new object[] { 2026, 1, 9, 1 };   // Friday
        yield return new object[] { 2026, 1, 10, 0 };  // Saturday
        yield return new object[] { 2026, 1, 11, 0 };  // Sunday
    }

    /// <summary>
    /// Provides single-day ranges in January 2026 paired with the number of non-working days each yields under the
    /// default working week (0 for a weekday, 1 for a weekend day).
    /// </summary>
    /// <returns>A sequence of <c>(int y, int m, int d, int expected)</c> rows.</returns>
    public static IEnumerable<object[]> SingleDayNonWorkingYieldRows()
    {
        yield return new object[] { 2026, 1, 5, 0 };   // Monday
        yield return new object[] { 2026, 1, 9, 0 };   // Friday
        yield return new object[] { 2026, 1, 10, 1 };  // Saturday
        yield return new object[] { 2026, 1, 11, 1 };  // Sunday
    }
}

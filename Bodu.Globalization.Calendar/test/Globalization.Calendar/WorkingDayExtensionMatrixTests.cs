// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 data-driven working-day classification and traversal matrices to the v2 explicit-service surface:
/// weekend/holiday/weekday classification across a calendar week, the next/previous working-day single step, the snap
/// family (including the equidistant tie that resolves forward), signed-day <see cref="NotableDateOnlyExtensions.AddWorkingDays" />
/// arithmetic, and inclusive <see cref="NotableDateOnlyExtensions.WorkingDaysBetween" /> counting.
/// </summary>
/// <remarks>
/// <para>
/// The fixture declares New Year's Day (1 January) as a non-working holiday so traversal must skip it. The
/// empty-rule January 2026 sweep relies on the calendar layout: 2026-01-05 is Monday through 2026-01-09 Friday, with
/// 2026-01-10 Saturday and 2026-01-11 Sunday the only non-working days, none of which coincide with the holiday.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// A fixture with a single non-working public holiday on 1 January.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.wd-matrix">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the fixture.
    /// </summary>
    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Provides every day of the first full business week of 2026 paired with its working-day classification under the
    /// default Monday-to-Friday working week.
    /// </summary>
    /// <returns>A sequence of <c>(int year, int month, int day, bool isWorkingDay)</c> rows.</returns>
    public static IEnumerable<object[]> WorkingDayClassificationRows()
    {
        yield return new object[] { 2026, 1, 5, true };   // Monday
        yield return new object[] { 2026, 1, 6, true };   // Tuesday
        yield return new object[] { 2026, 1, 7, true };   // Wednesday
        yield return new object[] { 2026, 1, 8, true };   // Thursday
        yield return new object[] { 2026, 1, 9, true };   // Friday
        yield return new object[] { 2026, 1, 10, false }; // Saturday
        yield return new object[] { 2026, 1, 11, false }; // Sunday
    }

    /// <summary>
    /// Provides signed-day samples for <see cref="NotableDateOnlyExtensions.AddWorkingDays" /> against the
    /// empty-weekend-only January 2026 week, covering zero, positive, negative and cross-week values.
    /// </summary>
    /// <returns>A sequence of <c>(int y, int m, int d, int days, int ey, int em, int ed)</c> rows.</returns>
    public static IEnumerable<object[]> AddWorkingDaysSignedRows()
    {
        yield return new object[] { 2026, 1, 5, 0, 2026, 1, 5 };    // zero returns input unchanged
        yield return new object[] { 2026, 1, 5, 1, 2026, 1, 6 };    // Monday + 1 -> Tuesday
        yield return new object[] { 2026, 1, 5, -1, 2026, 1, 2 };   // Monday - 1 -> previous Friday (cross weekend)
        yield return new object[] { 2026, 1, 5, 5, 2026, 1, 12 };   // Monday + 5 -> next Monday
        yield return new object[] { 2026, 1, 12, -5, 2026, 1, 5 };  // Monday - 5 -> previous Monday
    }

    /// <summary>
    /// Provides inclusive ranges in January 2026 and their expected working-day counts under the default Monday-to-Friday
    /// working week, including a single working day, a single weekend day, a full week, a multi-week span and a
    /// reversed-boundary pair that must yield the same count.
    /// </summary>
    /// <returns>A sequence of <c>(int sy, int sm, int sd, int ey, int em, int ed, int expected)</c> rows.</returns>
    public static IEnumerable<object[]> WorkingDaysBetweenRows()
    {
        yield return new object[] { 2026, 1, 5, 2026, 1, 5, 1 };    // single working day
        yield return new object[] { 2026, 1, 10, 2026, 1, 10, 0 };  // single weekend day
        yield return new object[] { 2026, 1, 5, 2026, 1, 11, 5 };   // one week -> five working days
        yield return new object[] { 2026, 1, 5, 2026, 1, 18, 10 };  // two weeks -> ten working days
        yield return new object[] { 2026, 1, 11, 2026, 1, 5, 5 };   // reversed boundaries -> symmetric count
    }
}

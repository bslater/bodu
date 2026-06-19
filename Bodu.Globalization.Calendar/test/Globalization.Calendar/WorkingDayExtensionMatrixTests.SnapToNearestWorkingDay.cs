// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToNearestWorkingDay" /> returns a working-day input unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        DateOnly input = new(2026, 1, 6);

        Assert.AreEqual(input, input.SnapToNearestWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the nearest snap chooses the closer side: a Saturday is one day from Friday and snaps backward,
    /// while a Sunday is one day from Monday and snaps forward.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expectedYear">The expected result year.</param>
    /// <param name="expectedMonth">The expected result month.</param>
    /// <param name="expectedDay">The expected result day.</param>
    [TestMethod]
    [DataRow(2026, 1, 3, 2026, 1, 2)]  // Saturday -> prior Friday
    [DataRow(2026, 1, 4, 2026, 1, 5)]  // Sunday -> next Monday
    public void SnapToNearestWorkingDay_WhenWeekend_ShouldChooseCloserSide(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        Assert.AreEqual(
            new DateOnly(expectedYear, expectedMonth, expectedDay),
            new DateOnly(year, month, day).SnapToNearestWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that when the forward and backward working days are equidistant the nearest snap prefers the forward
    /// result. With Wednesday 7 January 2026 marked non-working, Tuesday 6 and Thursday 8 are each one day away, so the
    /// forward Thursday wins.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenEquidistant_ShouldPreferForward()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.midweek">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="7" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(xml));

        Assert.AreEqual(new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 7).SnapToNearestWorkingDay(service, "XX"));
    }
}

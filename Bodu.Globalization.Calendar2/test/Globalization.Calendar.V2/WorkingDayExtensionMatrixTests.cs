// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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
public sealed class WorkingDayExtensionMatrixTests
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

    /// <summary>
    /// Verifies that a weekend day, a holiday, and an ordinary weekday are classified correctly. In 2026, 1 January is a
    /// Thursday (a non-working holiday), 3 January is a Saturday, and 2 January is an ordinary working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekendHolidayOrWeekday_ShouldClassifyEach()
    {
        INotableDateService service = Service;

        Assert.IsFalse(new DateOnly(2026, 1, 1).IsWorkingDay(service, "XX"), "1 January holiday");
        Assert.IsFalse(new DateOnly(2026, 1, 3).IsWorkingDay(service, "XX"), "Saturday");
        Assert.IsTrue(new DateOnly(2026, 1, 2).IsWorkingDay(service, "XX"), "ordinary weekday");
    }

    /// <summary>
    /// Verifies the working-day classification across every day of the first business week of 2026 under the default
    /// Monday-to-Friday working week.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected working-day classification.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WorkingDayClassificationRows), DynamicDataSourceType.Method)]
    public void IsWorkingDay_WhenScannedAcrossWeek_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the non-working classification across every day of the first business week of 2026 is the inverse
    /// of the working classification.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expectedWorking">The expected working-day classification whose inverse is asserted.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WorkingDayClassificationRows), DynamicDataSourceType.Method)]
    public void IsNonWorkingDay_WhenScannedAcrossWeek_ShouldReturnInverseOfWorkingClassification(int year, int month, int day, bool expectedWorking)
    {
        Assert.AreEqual(!expectedWorking, new DateOnly(year, month, day).IsNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsWeekend(DateOnly, WeekPattern?)" /> reports weekend days
    /// under the default working week and respects a custom working week.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenDefaultAndCustomWeek_ShouldClassifyByWorkingWeek()
    {
        Assert.IsTrue(new DateOnly(2026, 1, 10).IsWeekend(), "Saturday is a weekend by default");
        Assert.IsFalse(new DateOnly(2026, 1, 5).IsWeekend(), "Monday is not a weekend by default");

        // Under a Monday-to-Saturday working week, Saturday is a working day and Sunday is the only weekend day.
        Assert.IsFalse(new DateOnly(2026, 1, 10).IsWeekend(WeekPattern.MondayToSaturday), "Saturday is working in a six-day week");
        Assert.IsTrue(new DateOnly(2026, 1, 11).IsWeekend(WeekPattern.MondayToSaturday), "Sunday remains a weekend");
    }

    /// <summary>
    /// Verifies that a notable but working observance (a cultural day) remains a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayMatchesWorkingObservance_ShouldReturnTrue()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.cultural">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="cultural-day" displayName="Cultural Day" category="Cultural" defaultNonWorkingDay="false">
              <Rules><Rule id="x"><Strategy><Fixed month="June" day="5" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(xml));

        // 5 June 2026 is a Friday and is notable but working.
        Assert.IsTrue(new DateOnly(2026, 6, 5).IsWorkingDay(service, "XX"));
    }

    /// <summary>
    /// Verifies that the next working day after a Friday skips the weekend onto the following Monday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenFriday_ShouldReturnFollowingMonday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 2).NextWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next working day skips an intermediate non-working day. The fixture holiday on Thursday
    /// 1 January 2026 means the next working day after Wednesday 31 December 2025 is Friday 2 January 2026.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNextDayIsHoliday_ShouldSkipToFollowingWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2025, 12, 31).NextWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the previous working day before a Monday skips back across the weekend to the prior Friday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenMonday_ShouldReturnPriorFriday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5).PreviousWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the previous working day skips an intermediate non-working day. From Friday 2 January 2026 the
    /// previous working day skips the 1 January holiday and the preceding weekend to Wednesday 31 December 2025.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenPriorDayIsHoliday_ShouldSkipToEarlierWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2).PreviousWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that a working-day input is returned unchanged by every snap helper.
    /// </summary>
    [TestMethod]
    public void Snap_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        DateOnly input = new(2026, 1, 6);
        INotableDateService service = Service;

        Assert.AreEqual(input, input.SnapToWorkingDay(service, "XX"));
        Assert.AreEqual(input, input.SnapToWorkingDayBackward(service, "XX"));
        Assert.AreEqual(input, input.SnapToNearestWorkingDay(service, "XX"));
    }

    /// <summary>
    /// Verifies that a Saturday snaps forward to the following Monday and backward to the prior Friday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenSaturday_ShouldSnapForwardAndBackward()
    {
        DateOnly saturday = new(2026, 1, 3);
        INotableDateService service = Service;

        Assert.AreEqual(new DateOnly(2026, 1, 5), saturday.SnapToWorkingDay(service, "XX"));
        Assert.AreEqual(new DateOnly(2026, 1, 2), saturday.SnapToWorkingDayBackward(service, "XX"));
    }

    /// <summary>
    /// Verifies that the nearest snap chooses the closer side: a Saturday is one day from Friday and snaps backward,
    /// while a Sunday is one day from Monday and snaps forward.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenWeekend_ShouldChooseCloserSide()
    {
        INotableDateService service = Service;

        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3).SnapToNearestWorkingDay(service, "XX"), "Saturday -> prior Friday");
        Assert.AreEqual(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 4).SnapToNearestWorkingDay(service, "XX"), "Sunday -> next Monday");
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

    /// <summary>
    /// Verifies that signed-day arithmetic produces the expected working day across positive, negative, zero and
    /// cross-week values.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="days">The signed number of working days to add.</param>
    /// <param name="expectedYear">The expected result year.</param>
    /// <param name="expectedMonth">The expected result month.</param>
    /// <param name="expectedDay">The expected result day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(AddWorkingDaysSignedRows), DynamicDataSourceType.Method)]
    public void AddWorkingDays_WhenSignedDaysSupplied_ShouldReturnExpectedWorkingDay(int year, int month, int day, int days, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = new DateOnly(year, month, day).AddWorkingDays(days, Service, "XX");

        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Verifies that adding zero working days returns the input unchanged even when the input is a non-working holiday.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDaysOnHoliday_ShouldReturnInputUnchanged()
    {
        DateOnly holiday = new(2026, 1, 1);

        Assert.AreEqual(holiday, holiday.AddWorkingDays(0, Service, "XX"));
    }

    /// <summary>
    /// Verifies that adding working days skips the fixture holiday and the weekend. From Wednesday 31 December 2025,
    /// three working days are 2 January (Friday), 5 January (Monday) and 6 January (Tuesday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenCrossingHolidayAndWeekend_ShouldSkipNonWorkingDays()
    {
        INotableDateService service = Service;

        Assert.AreEqual(new DateOnly(2026, 1, 6), new DateOnly(2025, 12, 31).AddWorkingDays(3, service, "XX"));
        Assert.AreEqual(new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 6).AddWorkingDays(-3, service, "XX"));
    }

    /// <summary>
    /// Verifies the inclusive working-day count across single-day, full-week, multi-week and reversed-boundary ranges.
    /// </summary>
    /// <param name="startYear">The start year.</param>
    /// <param name="startMonth">The start month.</param>
    /// <param name="startDay">The start day.</param>
    /// <param name="endYear">The end year.</param>
    /// <param name="endMonth">The end month.</param>
    /// <param name="endDay">The end day.</param>
    /// <param name="expected">The expected inclusive working-day count.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WorkingDaysBetweenRows), DynamicDataSourceType.Method)]
    public void WorkingDaysBetween_WhenScannedAcrossRanges_ShouldReturnExpectedCount(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay, int expected)
    {
        int actual = new DateOnly(startYear, startMonth, startDay)
            .WorkingDaysBetween(new DateOnly(endYear, endMonth, endDay), Service, "XX");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that swapped boundaries produce a count equal to the in-order range, confirming the count is symmetric.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenBoundariesReversed_ShouldReturnSymmetricCount()
    {
        INotableDateService service = Service;

        int forward = new DateOnly(2026, 1, 5).WorkingDaysBetween(new DateOnly(2026, 1, 11), service, "XX");
        int reversed = new DateOnly(2026, 1, 11).WorkingDaysBetween(new DateOnly(2026, 1, 5), service, "XX");

        Assert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is excluded from the working-day count. The fixture holiday
    /// on 1 January 2026 falls inside 31 December 2025 to 7 January 2026 (4 weekday working days remain).
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenHolidayInRange_ShouldExcludeIt()
    {
        // 2025-12-31 Wed, 2026-01-01 Thu (holiday), 01-02 Fri, 01-03 Sat, 01-04 Sun, 01-05 Mon, 01-06 Tue, 01-07 Wed.
        // Working days: Wed 31, Fri 2, Mon 5, Tue 6, Wed 7 = 5; the holiday on Thursday is excluded.
        int count = new DateOnly(2025, 12, 31).WorkingDaysBetween(new DateOnly(2026, 1, 7), Service, "XX");

        Assert.AreEqual(5, count);
    }

    /// <summary>
    /// Verifies that a custom Sunday-to-Thursday working week treats Friday as a non-working day and Sunday as a working
    /// day. 15 May 2026 is a Friday and 17 May 2026 is a Sunday.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSundayToThursdayWeek_ShouldTreatFridayAsNonWorking()
    {
        INotableDateService service = Service;

        Assert.IsFalse(new DateOnly(2026, 5, 15).IsWorkingDay(service, "XX", WeekPattern.SundayToThursday), "Friday is non-working");
        Assert.IsTrue(new DateOnly(2026, 5, 17).IsWorkingDay(service, "XX", WeekPattern.SundayToThursday), "Sunday is working");
        Assert.IsTrue(new DateOnly(2026, 5, 14).IsWorkingDay(service, "XX", WeekPattern.SundayToThursday), "Thursday is working");
    }

    /// <summary>
    /// Verifies that the next working day under a Sunday-to-Thursday working week skips Friday and Saturday. From
    /// Thursday 14 May 2026 the next working day is Sunday 17 May 2026.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenSundayToThursdayWeek_ShouldSkipFridayAndSaturday()
    {
        Assert.AreEqual(new DateOnly(2026, 5, 17), new DateOnly(2026, 5, 14).NextWorkingDay(Service, "XX", WeekPattern.SundayToThursday));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).IsWorkingDay(null!, "XX");
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> territory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenTerritoryIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).IsWorkingDay(Service, null!);
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> from
    /// <see cref="NotableDateOnlyExtensions.AddWorkingDays" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).AddWorkingDays(1, null!, "XX");
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> from
    /// <see cref="NotableDateOnlyExtensions.WorkingDaysBetween" />.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).WorkingDaysBetween(new DateOnly(2026, 1, 11), null!, "XX");
        });
    }
}

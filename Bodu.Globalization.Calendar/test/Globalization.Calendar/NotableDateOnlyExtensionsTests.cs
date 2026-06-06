// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the working-day, traversal, and notable-date query extension methods over <see cref="DateOnly" />, using a
/// fixture with a fixed public holiday on 1 January.
/// </summary>
[TestClass]
public sealed class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// A fixture with a single non-working public holiday on 1 January.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.ext">
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
    /// Verifies that a non-working holiday, a weekend day, and an ordinary weekday are each classified correctly. In
    /// 2025, 1 January is a Wednesday holiday, 4 January is a Saturday, and 2 January is an ordinary working day.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected working-day classification.</param>
    [TestMethod]
    [DataRow(2025, 1, 1, false)]  // Wednesday holiday
    [DataRow(2025, 1, 4, false)]  // Saturday
    [DataRow(2025, 1, 2, true)]   // ordinary weekday
    public void IsWorkingDay_WhenDayIsHolidayWeekendOrWeekday_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next working day skips a holiday and the following weekend. 31 December 2024 is a Tuesday; the
    /// next working day skips the 1 January holiday (Wednesday) to land on Thursday 2 January 2025.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_SkipsHolidayAndWeekend()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 2), new DateOnly(2024, 12, 31).NextWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the previous working day skips the holiday and the preceding weekend. From Thursday 2 January 2025,
    /// the previous working day skips 1 January (holiday) and the 28-29 December weekend to Tuesday 31 December 2024.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_SkipsHolidayAndWeekend()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 2).PreviousWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that adding working days forward skips the holiday and weekends. From Tuesday 31 December 2024, three
    /// working days forward are 2 January (Thursday), 3 January (Friday) and 6 January (Monday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenAddingForward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 6), new DateOnly(2024, 12, 31).AddWorkingDays(3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that subtracting working days skips the holiday and weekends. From Monday 6 January 2025, three working
    /// days back are 3 January (Friday), 2 January (Thursday) and 31 December 2024 (Tuesday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenSubtractingBackward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 6).AddWorkingDays(-3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDay" /> moves the 1 January 2025 holiday forward
    /// to the following working day, Thursday 2 January.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenHoliday_ShouldReturnFollowingWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 2), new DateOnly(2025, 1, 1).SnapToWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDayBackward" /> moves the 1 January 2025 holiday
    /// backward to the prior working day, Tuesday 31 December 2024.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenHoliday_ShouldReturnPriorWorkingDay()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 1).SnapToWorkingDayBackward(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the inclusive working-day count over the first business week of 2025 excludes the holiday and the
    /// weekend.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_CountsInclusiveWorkingDays()
    {
        // 1-10 January 2025: Wed 1 (holiday), Thu 2, Fri 3, Sat 4, Sun 5, Mon 6, Tue 7, Wed 8, Thu 9, Fri 10 -> 7 working days.
        var count = new DateOnly(2025, 1, 1).WorkingDaysBetween(new DateOnly(2025, 1, 10), Service, "XX");

        Assert.AreEqual(7, count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsNotableDate" /> reports the holiday on its day and not on an
    /// ordinary day. In 2025, 1 January is the holiday and 2 January is an ordinary working day.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected notable-date classification.</param>
    [TestMethod]
    [DataRow(2025, 1, 1, true)]    // holiday
    [DataRow(2025, 1, 2, false)]   // ordinary day
    public void IsNotableDate_WhenDayIsHolidayOrOrdinary_ShouldReturnExpected(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsNotableDate(Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.GetNotableDates" /> returns the holiday occurrence on its day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenHolidayOnTheDay_ShouldReturnHoliday()
    {
        Assert.AreEqual("new-years-day", new DateOnly(2025, 1, 1).GetNotableDates(Service, "XX").Single().NotableDateId);
    }

    /// <summary>
    /// Verifies that the custom working week treats Saturday as a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WithSixDayWeek_TreatsSaturdayAsWorking()
    {
        Assert.IsTrue(new DateOnly(2025, 1, 4).IsWorkingDay(Service, "XX", WeekPattern.MondayToSaturday));
    }

    /// <summary>
    /// Verifies that the next non-working day from a working day lands on the holiday. Tuesday 31 December 2024's next
    /// non-working day is the 1 January 2025 holiday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenBeforeHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2024, 12, 31).NextNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the next non-working day from a mid-week working day lands on the weekend. Thursday 2 January 2025's
    /// next non-working day is Saturday 4 January.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenMidWeek_ShouldReturnSaturday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 4), new DateOnly(2025, 1, 2).NextNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that a six-day working week treats Saturday as working, so the next non-working day is the Sunday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WithSixDayWeek_ShouldSkipSaturday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 5), new DateOnly(2025, 1, 2).NextNonWorkingDay(Service, "XX", WeekPattern.MondayToSaturday));
    }

    /// <summary>
    /// Verifies that the previous non-working day from a working day lands on the holiday. Thursday 2 January 2025's
    /// previous non-working day is the 1 January holiday.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenAfterHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2).PreviousNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that enumerating non-working days over the first days of 2025 yields the holiday and the weekend.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_ShouldYieldHolidayAndWeekend()
    {
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 4), new DateOnly(2025, 1, 5) },
            new DateOnly(2025, 1, 1).EnumerateNonWorkingDays(new DateOnly(2025, 1, 5), Service, "XX").ToList());
    }

    /// <summary>
    /// Verifies that the next notable date strictly after a date returns the following year's recurrence.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenAfterOccurrence_ShouldReturnNextYearOccurrence()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 2).NextNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the next notable date is strictly after the reference date, skipping an occurrence that falls on it.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenOnOccurrence_ShouldSkipToNextYear()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 1).NextNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the previous notable date strictly before a date returns the current year's recurrence.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenAfterOccurrence_ShouldReturnSameYearOccurrence()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1).PreviousNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the next notable date is <see langword="null" /> when no occurrence exists up to the maximum year.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoFurtherOccurrence_ShouldReturnNull()
    {
        Assert.IsNull(new DateOnly(9999, 12, 31).NextNotableDate(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the previous notable date is <see langword="null" /> when no occurrence exists down to the minimum
    /// year.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenNoEarlierOccurrence_ShouldReturnNull()
    {
        Assert.IsNull(new DateOnly(1, 1, 1).PreviousNotableDate(Service, "XX"));
    }
}

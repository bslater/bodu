// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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
    /// Verifies that a weekend day, a holiday, and an ordinary weekday are classified correctly. In 2025, 1 January is a
    /// Wednesday (a non-working holiday), 4 January is a Saturday, and 2 January is an ordinary working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_ClassifiesWeekendHolidayAndWeekday()
    {
        INotableDateService service = Service;

        Assert.IsFalse(new DateOnly(2025, 1, 1).IsWorkingDay(service, "XX"), "1 January holiday");
        Assert.IsFalse(new DateOnly(2025, 1, 4).IsWorkingDay(service, "XX"), "Saturday");
        Assert.IsTrue(new DateOnly(2025, 1, 2).IsWorkingDay(service, "XX"), "ordinary weekday");
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
    /// Verifies that adding working days skips the holiday and weekends.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_SkipsNonWorkingDays()
    {
        // From Tuesday 31 December 2024, three working days: 2 Jan (Thu), 3 Jan (Fri), 6 Jan (Mon).
        Assert.AreEqual(new DateOnly(2025, 1, 6), new DateOnly(2024, 12, 31).AddWorkingDays(3, Service, "XX"));
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 6).AddWorkingDays(-3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that the snap helpers move a holiday to the surrounding working days.
    /// </summary>
    [TestMethod]
    public void Snap_MovesHolidayToWorkingDay()
    {
        DateOnly holiday = new(2025, 1, 1);
        INotableDateService service = Service;

        Assert.AreEqual(new DateOnly(2025, 1, 2), holiday.SnapToWorkingDay(service, "XX"));
        Assert.AreEqual(new DateOnly(2024, 12, 31), holiday.SnapToWorkingDayBackward(service, "XX"));
    }

    /// <summary>
    /// Verifies that the inclusive working-day count over the first business week of 2025 excludes the holiday and the
    /// weekend.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_CountsInclusiveWorkingDays()
    {
        // 1-10 January 2025: Wed 1 (holiday), Thu 2, Fri 3, Sat 4, Sun 5, Mon 6, Tue 7, Wed 8, Thu 9, Fri 10 -> 7 working days.
        int count = new DateOnly(2025, 1, 1).WorkingDaysBetween(new DateOnly(2025, 1, 10), Service, "XX");

        Assert.AreEqual(7, count);
    }

    /// <summary>
    /// Verifies that the date-level notable-date helpers report the holiday.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_ReturnsHolidayOnTheDay()
    {
        INotableDateService service = Service;

        Assert.IsTrue(new DateOnly(2025, 1, 1).IsNotableDate(service, "XX"));
        Assert.AreEqual("new-years-day", new DateOnly(2025, 1, 1).GetNotableDates(service, "XX").Single().NotableDateId);
        Assert.IsFalse(new DateOnly(2025, 1, 2).IsNotableDate(service, "XX"));
    }

    /// <summary>
    /// Verifies that the custom working week treats Saturday as a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WithSixDayWeek_TreatsSaturdayAsWorking()
    {
        Assert.IsTrue(new DateOnly(2025, 1, 4).IsWorkingDay(Service, "XX", WeekPattern.MondayToSaturday));
    }
}

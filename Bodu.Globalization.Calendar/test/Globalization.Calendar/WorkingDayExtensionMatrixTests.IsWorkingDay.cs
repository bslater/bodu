// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that a non-working holiday, a weekend day, and an ordinary weekday are each classified correctly under the
    /// default Monday-to-Friday working week. In 2026, 1 January is a Thursday holiday, 3 January is a Saturday, and
    /// 2 January is an ordinary working day.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected working-day classification.</param>
    [TestMethod]
    [DataRow(2026, 1, 1, false)]  // Thursday holiday
    [DataRow(2026, 1, 3, false)]  // Saturday
    [DataRow(2026, 1, 2, true)]   // ordinary weekday
    public void IsWorkingDay_WhenDayIsHolidayWeekendOrWeekday_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX"));
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
    [DynamicData(nameof(WorkingDayClassificationRows))]
    public void IsWorkingDay_WhenScannedAcrossWeek_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX"));
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
    /// Verifies that a custom Sunday-to-Thursday working week treats Friday as a non-working day while Sunday and Thursday
    /// remain working. In 2026, 15 May is a Friday, 17 May is a Sunday and 14 May is a Thursday.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected working-day classification.</param>
    [TestMethod]
    [DataRow(2026, 5, 15, false)]  // Friday is non-working
    [DataRow(2026, 5, 17, true)]   // Sunday is working
    [DataRow(2026, 5, 14, true)]   // Thursday is working
    public void IsWorkingDay_WhenSundayToThursdayWeek_ShouldReturnExpectedClassification(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsWorkingDay(Service, "XX", WeekPattern.SundayToThursday));
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
}

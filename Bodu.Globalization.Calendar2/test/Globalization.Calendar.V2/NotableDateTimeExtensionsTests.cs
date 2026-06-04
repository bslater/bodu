// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that <see cref="NotableDateTimeExtensions" /> delegates working-day logic to the date component while
/// preserving the time-of-day and kind on moved results.
/// </summary>
[TestClass]
public sealed class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// A resource with New Year's Day as a non-working holiday.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.dt">
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
    /// Verifies that the next non-working day lands on the holiday while preserving the time-of-day and kind.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_ShouldReturnHolidayPreservingTimeAndKind()
    {
        DateTime result = new DateTime(2024, 12, 31, 9, 30, 0, DateTimeKind.Utc).NextNonWorkingDay(Service, "XX");

        Assert.AreEqual(new DateTime(2025, 1, 1, 9, 30, 0, DateTimeKind.Utc), result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that the previous non-working day lands on the holiday while preserving the time-of-day.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_ShouldReturnHolidayPreservingTime()
    {
        DateTime result = new DateTime(2025, 1, 2, 8, 0, 0).PreviousNonWorkingDay(Service, "XX");

        Assert.AreEqual(new DateTime(2025, 1, 1, 8, 0, 0), result);
    }

    /// <summary>
    /// Verifies that enumerating non-working days reattaches the start's time-of-day to each yielded value.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_ShouldReattachStartTime()
    {
        List<DateTime> days = new DateTime(2025, 1, 1, 6, 15, 0)
            .EnumerateNonWorkingDays(new DateTime(2025, 1, 5, 0, 0, 0), Service, "XX")
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2025, 1, 1, 6, 15, 0),
                new DateTime(2025, 1, 4, 6, 15, 0),
                new DateTime(2025, 1, 5, 6, 15, 0),
            },
            days);
    }

    /// <summary>
    /// Verifies that the next notable date resolves from the date component, returning the next year's occurrence.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_ShouldReturnOccurrenceFromDateComponent()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateTime(2025, 1, 2, 23, 0, 0).NextNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the next working day skips the holiday and weekend while preserving the time-of-day and kind.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_ShouldSkipHolidayAndPreserveTimeAndKind()
    {
        // 31 Dec 2025 is a Wednesday; 1 Jan 2026 (Thursday) is the holiday, so the next working day is Friday 2 Jan.
        DateTime start = new(2025, 12, 31, 14, 30, 0, DateTimeKind.Utc);

        DateTime next = start.NextWorkingDay(Service, "XX");

        Assert.AreEqual(new DateOnly(2026, 1, 2), DateOnly.FromDateTime(next));
        Assert.AreEqual(new TimeSpan(14, 30, 0), next.TimeOfDay);
        Assert.AreEqual(DateTimeKind.Utc, next.Kind);
    }

    /// <summary>
    /// Verifies that the holiday is reported as a non-working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_OnHoliday_ShouldBeFalse()
    {
        DateTime holiday = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        Assert.IsFalse(holiday.IsWorkingDay(Service, "XX"));
        Assert.IsTrue(holiday.IsNonWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that the query method resolves the occurrence emitted on the date.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_OnHoliday_ShouldReturnOccurrence()
    {
        DateTime holiday = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

        Assert.AreEqual("new-years-day", holiday.GetNotableDates(Service, "XX").Single().NotableDateId);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that <see cref="NotableDateTimeOffsetExtensions" /> delegates working-day logic to the offset-local date
/// while preserving the time-of-day and offset on moved results.
/// </summary>
[TestClass]
public sealed class NotableDateTimeOffsetExtensionsTests
{
    /// <summary>
    /// A resource with New Year's Day as a non-working holiday.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.dto">
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
    /// Verifies that the next working day skips the holiday while preserving the time-of-day and offset.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_ShouldSkipHolidayAndPreserveTimeAndOffset()
    {
        DateTimeOffset start = new(2025, 12, 31, 14, 30, 0, TimeSpan.FromHours(5));

        DateTimeOffset next = start.NextWorkingDay(Service, "XX");

        Assert.AreEqual(new DateOnly(2026, 1, 2), DateOnly.FromDateTime(next.DateTime));
        Assert.AreEqual(new TimeSpan(14, 30, 0), next.TimeOfDay);
        Assert.AreEqual(TimeSpan.FromHours(5), next.Offset);
    }

    /// <summary>
    /// Verifies that the holiday is reported as a non-working day using the offset-local date.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_OnHoliday_ShouldBeFalse()
    {
        DateTimeOffset holiday = new(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(-8));

        Assert.IsFalse(holiday.IsWorkingDay(Service, "XX"));
    }
}

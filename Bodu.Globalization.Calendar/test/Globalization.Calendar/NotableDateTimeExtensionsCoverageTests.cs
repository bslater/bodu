// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the <see cref="DateTime" /> and <see cref="DateTimeOffset" /> notable-date extension wrappers delegate
/// to their <see cref="DateOnly" /> equivalents, preserving the time-of-day, kind, and offset.
/// </summary>
[TestClass]
public class NotableDateTimeExtensionsCoverageTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.dt-ext">
          <NotableDates>
            <NotableDate id="nyd" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="r">
                  <Applicability calendar="Gregorian"><Territory code="XX" /></Applicability>
                  <Strategy><Fixed month="January" day="1" /></Strategy>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> wrappers resolve notable-date, weekend, working-day, snap, and
    /// counting queries.
    /// </summary>
    [TestMethod]
    public void DateTimeExtensions_ShouldResolveQueries()
    {
        INotableDateService service = Service;
        var holiday = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);   // Thursday public holiday
        var later = new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc);

        Assert.IsTrue(holiday.IsNotableDate(service, "XX"));
        Assert.IsFalse(holiday.IsWeekend());
        Assert.IsNotNull(holiday.EnumerateNotableDates(later, service, "XX"));
        _ = holiday.PreviousNotableDate(service, "XX");

        // 1 January is a non-working holiday, so working-day snaps and steps move off it.
        Assert.AreEqual(DateTimeKind.Utc, holiday.PreviousWorkingDay(service, "XX").Kind);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToWorkingDay(service, "XX").Date);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToWorkingDayBackward(service, "XX").Date);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToNearestWorkingDay(service, "XX").Date);
        Assert.IsTrue(holiday.WorkingDaysBetween(later, service, "XX") > 0);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> wrappers resolve notable-date, weekend, working-day, snap, and
    /// counting queries.
    /// </summary>
    [TestMethod]
    public void DateTimeOffsetExtensions_ShouldResolveQueries()
    {
        INotableDateService service = Service;
        TimeSpan offset = TimeSpan.FromHours(10);
        var holiday = new DateTimeOffset(2026, 1, 1, 9, 0, 0, offset);   // Thursday public holiday
        var later = new DateTimeOffset(2026, 1, 12, 9, 0, 0, offset);

        Assert.IsTrue(holiday.IsNotableDate(service, "XX"));
        Assert.IsFalse(holiday.IsWeekend());
        Assert.IsTrue(holiday.IsNonWorkingDay(service, "XX"));
        Assert.IsNotNull(holiday.GetNotableDates(service, "XX"));
        Assert.IsNotNull(holiday.EnumerateNotableDates(later, service, "XX"));

        Assert.AreEqual(offset, holiday.PreviousWorkingDay(service, "XX").Offset);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToWorkingDay(service, "XX").Date);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToWorkingDayBackward(service, "XX").Date);
        Assert.AreNotEqual(holiday.Date, holiday.SnapToNearestWorkingDay(service, "XX").Date);
        Assert.IsTrue(holiday.WorkingDaysBetween(later, service, "XX") > 0);
    }
}

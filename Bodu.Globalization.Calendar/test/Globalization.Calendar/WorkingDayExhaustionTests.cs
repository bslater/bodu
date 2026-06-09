// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExhaustionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the working-day and non-working-day traversal helpers throw when no qualifying day can be found within
/// the traversal guard.
/// </summary>
[TestClass]
public class WorkingDayExhaustionTests
{
    // A resource whose only holiday applies to territory "ZZ", so territory "XX" has no non-working holidays.
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.exhaust">
          <NotableDates>
            <NotableDate id="zz" displayName="ZZ Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="r">
                  <Applicability calendar="Gregorian"><Territory code="ZZ" /></Applicability>
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
    /// Verifies that the next-working-day search throws when no day of the week is a working day.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNoDayIsWorking_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new DateOnly(2025, 6, 2).NextWorkingDay(Service, "XX", WeekPattern.Empty);
        });
    }

    /// <summary>
    /// Verifies that the next-non-working-day search throws when every day is a working day and no holiday applies.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenEveryDayIsWorking_ShouldThrowInvalidOperationException()
    {
        var allWorking = new WeekPattern(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new DateOnly(2025, 6, 2).NextNonWorkingDay(Service, "XX", allWorking);
        });
    }
}

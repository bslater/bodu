// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthoringCompanyHolidays.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates authoring a calendar from scratch with the fluent builder: notable-date concepts,
/// per-concept rules using the declarative date strategies (fixed date, nth weekday of month), and
/// the immediate build-to-service path. Rules are data, not code — the same document could equally
/// have been written as XML by hand.
/// </summary>
public static class AuthoringCompanyHolidays
{
    /// <summary>
    /// Authors a small company calendar and queries it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Authoring company holidays with the fluent builder ---");

        // A company calendar: a fixed founding day, a floating summer party (first Friday of
        // December - southern hemisphere summer), and a year-end shutdown whose boundaries are
        // computed from the calendar rather than hard-coded. The shutdown's last working day is
        // the Friday before Boxing Day (26 Dec), and staff return on the first Monday after New
        // Year's Day - two weekday-relative rules the WeekdayNearDate strategy expresses directly.
        NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso-au-holidays")
            .WithMetadata("Contoso AU holidays", "Company-observed days for Contoso Australia")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)))
            .AddNotableDate("summer-party", "Summer Party", NotableDateCategory.Cultural, c => c
                .AddRule("first-friday", r => r.DayOfWeekInMonth(12, DayOfWeek.Friday, WeekOrdinal.First)))
            .AddNotableDate("shutdown-last-day", "Shutdown - Last Working Day", NotableDateCategory.Other, c => c
                .AddRule("friday-before-boxing-day", r => r.WeekdayNearDate(12, 26, DayOfWeek.Friday, WeekdayProximity.Before)))
            .AddNotableDate("shutdown-return", "Shutdown - Return to Work", NotableDateCategory.Other, c => c
                .AddRule("first-monday-after-new-year", r => r.WeekdayNearDate(1, 1, DayOfWeek.Monday, WeekdayProximity.After)))
            .Build();

        // The resource is a first-class calendar - the same service type the data packs return.
        var service = new NotableDateService(resource);

        foreach (NotableDate date in service.Resolve(2024, "AU"))
            Console.WriteLine($"  {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-27} " +
                $"{date.Category}, non-working: {date.IsNonWorkingDay}, spans {date.DurationDays}d");

        // Compose the two boundary markers into the actual shutdown window. The last working day
        // sits in December; the matching return-to-work falls in the following January, so the
        // span crosses the year boundary and its length varies year to year.
        DateOnly lastWorkingDay = service
            .Resolve(new DateRange(new DateOnly(2024, 12, 1), new DateOnly(2024, 12, 31)), "AU")
            .Single(d => d.NotableDateId == "shutdown-last-day").Date;
        DateOnly returnToWork = service
            .Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31)), "AU")
            .Single(d => d.NotableDateId == "shutdown-return").Date;

        Console.WriteLine();
        Console.WriteLine($"  Year-end shutdown: last worked {lastWorkingDay:yyyy-MM-dd} ({lastWorkingDay.DayOfWeek}), " +
            $"closed {lastWorkingDay.AddDays(1):yyyy-MM-dd} to {returnToWork.AddDays(-1):yyyy-MM-dd}, " +
            $"back {returnToWork:yyyy-MM-dd} ({returnToWork.DayOfWeek})");

        Console.WriteLine();
    }
}

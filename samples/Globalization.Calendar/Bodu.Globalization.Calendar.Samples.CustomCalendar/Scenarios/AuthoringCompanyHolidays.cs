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
        // December - southern hemisphere summer), and a year-end shutdown spanning several days.
        NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso-au-holidays")
            .WithMetadata("Contoso AU holidays", "Company-observed days for Contoso Australia")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)))
            .AddNotableDate("summer-party", "Summer Party", NotableDateCategory.Cultural, c => c
                .AddRule("first-friday", r => r.DayOfWeekInMonth(12, DayOfWeek.Friday, WeekOrdinal.First)))
            .AddNotableDate("shutdown", "Year-End Shutdown", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(12, 27).WithDurationDays(3)))
            .Build();

        // The resource is a first-class calendar - the same service type the data packs return.
        var service = new NotableDateService(resource);

        foreach (NotableDate date in service.Resolve(2024, "AU"))
            Console.WriteLine($"  {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-22} " +
                $"{date.Category}, non-working: {date.IsNonWorkingDay}, spans {date.DurationDays}d");

        Console.WriteLine();
    }
}

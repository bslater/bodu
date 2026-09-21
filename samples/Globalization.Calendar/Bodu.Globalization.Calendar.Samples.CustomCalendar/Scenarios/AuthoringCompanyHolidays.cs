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
/// Demonstrates authoring a calendar from scratch with the fluent builder: notable-date concepts, per-concept rules
/// using the declarative date strategies, a recurrence source, and a calculated end-date duration, plus the immediate
/// build-to-service path. Rules are data, not code — the same document could equally have been written as XML by hand.
/// </summary>
public static class AuthoringCompanyHolidays
{
    /// <summary>
    /// Authors a small company calendar and queries it.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Authoring a calendar from scratch",
            what: "Builds a company calendar with three concepts - a fixed founding day, a fortnightly all-hands "
                + "from a recurrence source, and a year-end shutdown whose span is calculated - then resolves "
                + "thirteen months so the cross-year shutdown is visible whole.",
            why: "The engine exists to answer questions about holidays nobody tabulated, and that has to include "
                + "a company's own. The point of the builder is that it produces the same document a data pack "
                + "ships: rules are data, so this calendar could equally have been hand-written as XML and "
                + "committed, and the resulting service is the same type the regional packs return. The shutdown "
                + "is the interesting concept because its length is not a constant - it runs from the last "
                + "working day before Boxing Day to the day before business resumes, so its duration changes "
                + "every year and it crosses the year boundary. Expressing that declaratively rather than as code "
                + "is what the calculated-duration strategies are for.",
            expect: "One document, three concepts, and occurrences that differ in kind: a single fixed date, a "
                + "recurring event appearing many times, and a multi-day span reporting its own duration and end "
                + "date. The shutdown spans December into January from one occurrence rather than two.");

        // A company calendar: a fixed founding day, a fortnightly all-hands (a recurrence source), and a year-end
        // shutdown authored as a single concept whose span is calculated. The shutdown starts the Friday before Boxing
        // Day (exclusive - that Friday is the last day worked) and ends the day before the first Monday after New Year's
        // Day (exclusive), so its length varies year to year and it crosses the year boundary.
        NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso-au-holidays")
            .WithMetadata("Contoso AU holidays", "Company-observed days for Contoso Australia")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)))
            .AddNotableDate("all-hands", "Fortnightly All-Hands", NotableDateCategory.Other, c => c
                .AddRule("fortnightly", r => r.DailyInterval(new DateOnly(2024, 1, 5), 14)))
            .AddNotableDate("year-end-shutdown", "Year-End Shutdown", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("default", r => r
                    .WeekdayNearDate(12, 26, DayOfWeek.Friday, WeekdayProximity.Before)
                    .UntilDate(
                        end => end.WeekdayNearDate(1, 1, DayOfWeek.Monday, WeekdayProximity.After),
                        startBoundary: DateBoundary.Exclusive,
                        endBoundary: DateBoundary.Exclusive,
                        selection: EndDateSelection.FirstOnOrAfterStart)))
            .Build();

        // The resource is a first-class calendar - the same service type the data packs return.
        var service = new NotableDateService(resource);

        // Resolve December 2024 into January 2025 so the cross-year shutdown span is visible in full.
        foreach (NotableDate date in service.Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 31)), "AU"))
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-22} "
                + $"{date.Category}, non-working: {date.IsNonWorkingDay}, spans {date.DurationDays}d (ends {date.EndDate:yyyy-MM-dd})");

        Console.WriteLine("  (three kinds of occurrence from one document: a fixed date, a recurring event repeating many times, and a multi-day span that reports its own duration)");

        Console.WriteLine();
    }
}

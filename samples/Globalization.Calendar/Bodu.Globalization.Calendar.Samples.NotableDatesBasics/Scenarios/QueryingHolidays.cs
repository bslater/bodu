// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QueryingHolidays.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates the primary query surface: create a ready-to-use service from a regional data pack
/// and resolve notable dates for a whole year, a single day, and an arbitrary range.
/// </summary>
public static class QueryingHolidays
{
    /// <summary>
    /// Queries Australian national holidays for 2024 through each resolve shape.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Querying holidays from a data pack ---");

        // One call: the data pack loads the embedded per-country XML (with its shared catalogue
        // imports resolved) and returns a ready, immutable, thread-safe service.
        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // Whole civil year (extension overload). Every resolved occurrence carries its concept id,
        // display name, category, and the working-day consequence.
        IReadOnlyList<NotableDate> year2024 = service.Resolve(2024, "AU");
        Console.WriteLine($"AU 2024: {year2024.Count} notable dates. First five:");
        foreach (NotableDate date in year2024.Take(5))
            Console.WriteLine($"  {date.Date:yyyy-MM-dd} {date.DisplayName,-22} ({date.Category}, non-working: {date.IsNonWorkingDay})");

        // Single day: "is anything notable on this date?"
        IReadOnlyList<NotableDate> anzacDay = service.Resolve(new DateOnly(2024, 4, 25), "AU");
        Console.WriteLine($"2024-04-25: {string.Join(", ", anzacDay.Select(d => d.DisplayName))}");

        // Arbitrary range: the Easter block, spanning March/April.
        IReadOnlyList<NotableDate> easter = service.Resolve(
            new DateRange(new DateOnly(2024, 3, 29), new DateOnly(2024, 4, 1)), "AU");
        Console.WriteLine($"Easter window: {string.Join(", ", easter.Select(d => $"{d.DisplayName} ({d.Date:MM-dd})"))}");

        // The pack also reports what it can answer for.
        Console.WriteLine($"Supported territories in the AU resource: {service.GetSupportedTerritories().Count}");
        Console.WriteLine($"AsiaPacific pack countries: {string.Join(", ", AsiaPacificCalendarData.SupportedCountries)}");

        Console.WriteLine();
    }
}

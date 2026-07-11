// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilteringAndCategories.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates <see cref="NotableDateFilter" />: immutable, composable predicates that push the
/// "which dates count?" question into the query instead of post-filtering. Filters combine with
/// <c>And</c>/<c>Or</c>/<c>Not</c>, so a policy like "public holidays that actually stop work" is one
/// reusable value.
/// </summary>
public static class FilteringAndCategories
{
    /// <summary>
    /// Runs the same year query under increasingly specific filters.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Composable filters and categories ---");

        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // Unfiltered: every notable date the rules emit (public holidays, observances, ...).
        var all = service.Resolve(2024, "AU");

        // Category filter: only the public holidays.
        var publicHolidays = service.Resolve(2024, "AU", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

        // Composed: public holidays that are also non-working days - the payroll-relevant set.
        NotableDateFilter daysOff = NotableDateFilter
            .ForCategory(NotableDateCategory.PublicHoliday)
            .And(NotableDateFilter.IsNonWorkingDay());
        var nonWorking = service.Resolve(2024, "AU", daysOff);

        Console.WriteLine($"AU 2024 - all: {all.Count}, public holidays: {publicHolidays.Count}, non-working public holidays: {nonWorking.Count}");

        // Negation: notable dates that do NOT stop work (observances, commemorations).
        var observances = service.Resolve(2024, "AU", NotableDateFilter.IsNonWorkingDay().Not());
        Console.WriteLine($"Working-day observances: {string.Join(", ", observances.Take(4).Select(d => d.DisplayName))}, ...");

        // Name filter: track one concept across the year (multi-rule concepts can emit several).
        var christmas = service.Resolve(2024, "AU", NotableDateFilter.WithName("Christmas Day"));
        foreach (NotableDate date in christmas)
            Console.WriteLine($"Christmas Day 2024: {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek})");

        Console.WriteLine();
    }
}

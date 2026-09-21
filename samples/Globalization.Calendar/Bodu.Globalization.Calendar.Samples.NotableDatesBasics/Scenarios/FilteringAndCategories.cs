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
        SampleConsole.Scenario(
            "Composable filters and categories",
            what: "Runs the same year query unfiltered, filtered to public holidays, filtered to public holidays "
                + "that are also non-working, negated to the dates that do not stop work, and filtered by "
                + "concept name.",
            why: "The question 'which dates count?' has a different answer for payroll, for a delivery estimate "
                + "and for a greeting-card app, and none of them is 'all of them'. Making the filter a value "
                + "rather than a post-query loop means that policy can be named once, passed around, and reused "
                + "across every query - and it lets the engine skip work rather than computing occurrences the "
                + "caller is about to discard. The composition operators matter because real policies are "
                + "conjunctions: 'public holiday' is not the same set as 'day off', since a public holiday that "
                + "is not a non-working day exists in several territories.",
            expect: "The counts narrow from every notable date to the public holidays. The last two coincide "
                + "here because every Australian public holiday in 2024 is also a day off - they are still "
                + "different filters, and in territories with a working public holiday the counts diverge, "
                + "which is exactly why composing them is worth the trouble. Negation gives the complementary "
                + "view rather than a second filter to keep in sync. The name filter can return more than one "
                + "occurrence for one concept, because a concept may be emitted by several rules.");

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

        Console.WriteLine($"  AU 2024 - all: {all.Count}, public holidays: {publicHolidays.Count}, non-working public holidays: {nonWorking.Count}"
            + "  (the last two coincide here - every AU public holiday in 2024 is also a day off - but they are different filters, and elsewhere the counts diverge)");

        // Negation: notable dates that do NOT stop work (observances, commemorations).
        var observances = service.Resolve(2024, "AU", NotableDateFilter.IsNonWorkingDay().Not());
        Console.WriteLine($"  Working-day observances: {string.Join(", ", observances.Take(4).Select(d => d.DisplayName))}, ..."
            + "  (the complement of the previous filter via Not(), rather than a second filter to keep in sync with it)");

        // Name filter: track one concept across the year (multi-rule concepts can emit several).
        var christmas = service.Resolve(2024, "AU", NotableDateFilter.WithName("Christmas Day"));
        foreach (NotableDate date in christmas)
            Console.WriteLine($"  Christmas Day 2024: {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek})"
                + "  (one row here, but a name filter can return several - a concept may be emitted by more than one rule)");

        Console.WriteLine();
    }
}

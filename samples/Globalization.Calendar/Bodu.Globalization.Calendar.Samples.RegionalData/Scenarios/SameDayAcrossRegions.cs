// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SameDayAcrossRegions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.RegionalData.Scenarios;

/// <summary>
/// Demonstrates that one calendar date means different things in different territories, which is the reason a
/// notable-date lookup is always territory-scoped rather than global.
/// </summary>
public static class SameDayAcrossRegions
{
    /// <summary>
    /// Asks several territories what 1 January and 25 December 2026 are, and shows a weekend substitution in action.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "One date, five territories - why the lookup is territory-scoped",
            what: "Resolves 1 January and 25 December 2026 in a territory from each region pack, printing what each " +
                  "one calls the day and whether it is a non-working day there.",
            why: "A global 'is it a holiday' function cannot be written, and the failure mode is a silent one: a " +
                  "payroll or SLA calculation that assumes 25 December is universally a day off is simply wrong in " +
                  "much of the world, and wrong in a way that only shows up in the affected region's data. Scoping " +
                  "every query to a territory forces the caller to say which calendar they mean, and the answer " +
                  "carries the territory back so it cannot be mistaken later.",
            expect: "1 January is a public holiday in every territory shown. 25 December is not: it resolves as a " +
                    "public holiday in the US, GB, AU, and ZA, but the AE row shows no public holiday for that date " +
                    "at all - which is the point of the scenario, not a gap in the data.");

        var territories = new (string Region, string Territory, Func<string, NotableDateService> Factory)[]
        {
            ("Americas",    "US", AmericasCalendarData.CreateService),
            ("Europe",      "GB", EuropeCalendarData.CreateService),
            ("AsiaPacific", "AU", AsiaPacificCalendarData.CreateService),
            ("MiddleEast",  "AE", MiddleEastCalendarData.CreateService),
            ("Africa",      "ZA", AfricaCalendarData.CreateService),
        };

        foreach (var date in new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 25) })
        {
            Console.WriteLine($"  {date:yyyy-MM-dd} ({date.DayOfWeek}):");

            foreach (var (region, territory, factory) in territories)
            {
                var service = factory(territory);
                var resolved = service.Resolve(date, territory);
                var holidays = resolved.Where(static d => d.Category == NotableDateCategory.PublicHoliday).ToList();

                var description = holidays.Count == 0
                    ? "(no public holiday here)"
                    : string.Join(", ", holidays.Select(static h => $"{h.DisplayName}{(h.IsNonWorkingDay ? "" : " [working day]")}"));

                Console.WriteLine($"    {territory} ({region,-11}) : {description}");
            }

            Console.WriteLine();
        }

        // Weekend substitution: when a fixed-date holiday lands on a weekend, many territories move the day off.
        // The occurrence carries both dates, so an audit can see what moved and why.
        var au = AsiaPacificCalendarData.CreateService("AU");
        var observed = au
            .Resolve(2027, "AU", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday))
            .Where(static d => d.IsObserved)
            .ToList();

        Console.WriteLine($"  AU 2027 substituted holidays ({observed.Count}):");
        foreach (var d in observed)
        {
            Console.WriteLine($"    {d.Date:yyyy-MM-dd ddd}  {d.DisplayName,-28} actual {d.ActualDate:yyyy-MM-dd ddd}  reason: {d.AdjustmentReason ?? "(none recorded)"}");
        }

        Console.WriteLine("    (IsObserved marks a shifted day; ActualDate keeps the calendar date it shifted from, so the move is auditable rather than implied)");
        Console.WriteLine();
    }
}

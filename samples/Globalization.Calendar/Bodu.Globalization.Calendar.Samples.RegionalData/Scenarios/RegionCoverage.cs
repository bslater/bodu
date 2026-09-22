// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegionCoverage.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.RegionalData.Scenarios;

/// <summary>
/// Demonstrates what each of the five regional data packs ships: the territories it covers, and the fact that every
/// one of them loads and resolves without any file on disk or network call.
/// </summary>
public static class RegionCoverage
{
    /// <summary>
    /// Lists each region pack's supported territories and resolves a year for one territory from each.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Five region packs - what each one covers",
            what: "Asks every regional data pack for its supported territories, then loads one service per region " +
                  "and counts the public holidays it resolves for 2026.",
            why: "The packs exist so an application pays for the territories it actually needs. The rules for 60-odd " +
                  "territories, each with its own subdivisions and its own religious calendars, are far larger than " +
                  "any one deployment wants embedded; splitting them by region turns that into an explicit " +
                  "dependency choice. Each pack is a self-contained embedded resource, so there is no data directory " +
                  "to deploy, no download on first use, and no ambient state - which is what makes a holiday " +
                  "calculation reproducible on a build agent.",
            expect: "Every region reports a non-empty territory list and a non-zero public-holiday count for its " +
                    "sample territory, and the totals below add up to the same number of territories the packs " +
                    "declare. Nothing here touches the filesystem or the network.");

        var regions = new (string Region, IReadOnlyList<string> Countries, string Sample, Func<string, NotableDateService> Factory)[]
        {
            ("Americas",    AmericasCalendarData.SupportedCountries,    "US", AmericasCalendarData.CreateService),
            ("AsiaPacific", AsiaPacificCalendarData.SupportedCountries, "AU", AsiaPacificCalendarData.CreateService),
            ("Europe",      EuropeCalendarData.SupportedCountries,      "GB", EuropeCalendarData.CreateService),
            ("MiddleEast",  MiddleEastCalendarData.SupportedCountries,  "AE", MiddleEastCalendarData.CreateService),
            ("Africa",      AfricaCalendarData.SupportedCountries,      "ZA", AfricaCalendarData.CreateService),
        };

        var total = 0;

        foreach (var (region, countries, sample, factory) in regions)
        {
            // Each pack is embedded in its own assembly: creating the service reads no file and makes no request.
            var service = factory(sample);
            var holidays = service.Resolve(2026, sample, NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

            total += countries.Count;

            Console.WriteLine($"  {region,-12} {countries.Count,2} territories  [{string.Join(", ", countries)}]");
            Console.WriteLine($"  {"",-12} {sample} 2026: {holidays.Count} public holidays");
        }

        Console.WriteLine();
        Console.WriteLine($"  territories across all five packs : {total}   (reference only the regions you ship to - the embedded rules travel with the assembly)");
        Console.WriteLine();
    }
}

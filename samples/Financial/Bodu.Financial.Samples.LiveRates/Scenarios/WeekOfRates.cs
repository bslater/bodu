// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekOfRates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.LiveRates.Scenarios;

/// <summary>
/// Reads a whole week of published rates in one range call. The seven-day window ending on the
/// buffered Wednesday always contains at least four business days, so the range is never empty; the
/// weekend days simply have no observations, which is normal business-day data.
/// </summary>
public static class WeekOfRates
{
    /// <summary>
    /// Fetches the week's observations and prints one line per published fixing.
    /// </summary>
    /// <param name="provider">The warmed live provider.</param>
    /// <param name="fromIso">The source ISO code.</param>
    /// <param name="toIso">The destination ISO code.</param>
    /// <param name="startDate">The inclusive window start.</param>
    /// <param name="endDate">The inclusive window end (the computed Wednesday).</param>
    public static void Run(IDatedRateProvider provider, string fromIso, string toIso, DateOnly startDate, DateOnly endDate)
    {
        SampleConsole.Scenario(
            $"A week of rates: {fromIso}/{toIso} {startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}",
            what: "Fetches a date range from the live provider and lists every observation in it with its "
                + "weekday and publisher.",
            why: "A range fetch is a different operation from repeated single lookups, and the reason is cost: "
                + "most feeds serve a span in one request, so asking day by day multiplies the network calls by "
                + "the length of the window for the same data. The listing shows the other thing worth knowing "
                + "about rate data, which is that it is sparse - there is no fixing on a weekend, so a week "
                + "yields fewer observations than it has days, and code that assumes one rate per calendar day "
                + "is wrong about every Saturday.",
            expect: "Fewer observations than days in the window, with the gaps falling on the weekend - which is "
                + "the shape all rate data has and the reason the lookup modes in the offline sample exist. The "
                + "values are live, so they will not match any recorded transcript.");

        // One call returns every observation the source published inside the window, ordered by
        // date. Days without a fixing (weekends, holidays) are simply absent - count the rows,
        // don't assume seven.
        RateRangeResult week = provider.GetRates(fromIso, toIso, startDate, endDate);

        Console.WriteLine($"  {week.Count} observations in the window:");
        foreach (ExchangeRate rate in week.Rates)
            Console.WriteLine($"  {rate.Date:yyyy-MM-dd} ({rate.Date.DayOfWeek,-9}) {rate.Rate}  [{rate.Provider}]");

        Console.WriteLine();
    }
}

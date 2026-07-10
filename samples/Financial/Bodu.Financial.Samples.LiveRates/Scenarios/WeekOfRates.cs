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
        Console.WriteLine($"--- A week of rates: {fromIso}/{toIso} {startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd} ---");

        // One call returns every observation the source published inside the window, ordered by
        // date. Days without a fixing (weekends, holidays) are simply absent - count the rows,
        // don't assume seven.
        RateRangeResult week = provider.GetRates(fromIso, toIso, startDate, endDate);

        Console.WriteLine($"{week.Count} observations in the window:");
        foreach (ExchangeRate rate in week.Rates)
            Console.WriteLine($"  {rate.Date:yyyy-MM-dd} ({rate.Date.DayOfWeek,-9}) {rate.Rate}  [{rate.Provider}]");

        Console.WriteLine();
    }
}

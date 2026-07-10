// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HistoricalDate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.LiveRates.Scenarios;

/// <summary>
/// Resolves the published rate for one computed historical date. The date is always a Wednesday at
/// least five days in the past, and the lookup carries a <see cref="RateLookupOptions.PreviousWithin" />
/// tolerance, so a result is virtually certain even if that Wednesday was a market holiday.
/// </summary>
public static class HistoricalDate
{
    /// <summary>
    /// Looks up the single-date rate and prints the resolution and provenance detail.
    /// </summary>
    /// <param name="provider">The warmed live provider.</param>
    /// <param name="fromIso">The source ISO code.</param>
    /// <param name="toIso">The destination ISO code.</param>
    /// <param name="date">The computed target date (last Wednesday, buffered).</param>
    public static void Run(IDatedRateProvider provider, string fromIso, string toIso, DateOnly date)
    {
        Console.WriteLine($"--- Single historical date: {fromIso}/{toIso} on {date:yyyy-MM-dd} ---");

        // PreviousWithin(5) is the safety net: if the Wednesday itself had no fixing (a rare
        // mid-week holiday), the most recent prior business day answers instead - and the result
        // says so via Resolution/OffsetDays, so nothing is silently substituted.
        RateLookupResult result = provider.GetRate(fromIso, toIso, date, RateLookupOptions.PreviousWithin(5));

        Console.WriteLine($"Rate            : {result.Rate.Rate}");
        Console.WriteLine($"Observed on     : {result.Rate.Date:yyyy-MM-dd} ({result.Resolution}, offset {result.OffsetDays}d)");
        Console.WriteLine($"Published by    : {result.Rate.Provider}");
        Console.WriteLine($"Origin          : {result.Provenance.Origin}");
        Console.WriteLine();
    }
}

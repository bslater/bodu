// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HistoryClamping.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.CachedRates.Scenarios;

/// <summary>
/// Demonstrates <see cref="RateHistoryAvailability" />: a provider's declaration of how far back its
/// history reaches. The caching and aggregation layers consume this declaration (their
/// <c>RespectHistoryAvailability</c> options default to <see langword="true" />) to clamp range fetches
/// and skip providers that cannot possibly serve a date — instead of issuing doomed requests.
/// </summary>
public static class HistoryClamping
{
    /// <summary>
    /// Evaluates the three availability kinds against sample dates.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- History availability: Unbounded / Since / RollingDays ---");

        // "As of" is explicit so the demonstration is deterministic.
        var asOf = new DateOnly(2024, 7, 1);
        var recent = new DateOnly(2024, 6, 3);
        var old = new DateOnly(1990, 1, 15);

        // Unbounded: archival feeds (e.g. OFX). Everything is claimed available.
        RateHistoryAvailability unbounded = RateHistoryAvailability.Unbounded;

        // Since: feeds with a fixed inception, like the ECB reference rates (published since 1999).
        RateHistoryAvailability since = RateHistoryAvailability.Since(new DateOnly(1999, 1, 4));

        // RollingDays: feeds that only keep a moving window, like OANDA's anonymous ~180-day API.
        RateHistoryAvailability rolling = RateHistoryAvailability.RollingDays(180);

        Console.WriteLine($"{"kind",-22} {"earliest as of " + asOf.ToString("yyyy-MM-dd"),-26} {recent:yyyy-MM-dd}  {old:yyyy-MM-dd}");
        Print("Unbounded", unbounded, asOf, recent, old);
        Print("Since(1999-01-04)", since, asOf, recent, old);
        Print("RollingDays(180)", rolling, asOf, recent, old);

        // A provider built from loaded data declares its own availability - the offline sample
        // source reports Since(<first observation>).
        FixedDatedRateProvider sourceProvider = StaticRates.LoadAudDaily();
        RateHistoryAvailability declared = sourceProvider.HistoryAvailability;
        Console.WriteLine($"Offline source declares: {declared.Kind}, earliest {declared.EarliestDate:yyyy-MM-dd}");

        Console.WriteLine();
    }

    /// <summary>
    /// Prints one availability declaration: its earliest reachable date and whether it can serve the
    /// two probe dates.
    /// </summary>
    /// <param name="label">The row label.</param>
    /// <param name="availability">The declaration to evaluate.</param>
    /// <param name="asOf">The evaluation instant.</param>
    /// <param name="recent">A recent probe date.</param>
    /// <param name="old">A deep-history probe date.</param>
    private static void Print(string label, RateHistoryAvailability availability, DateOnly asOf, DateOnly recent, DateOnly old)
    {
        DateOnly? earliest = availability.GetEarliestAvailable(asOf);
        Console.WriteLine(
            $"{label,-22} {(earliest is null ? "(no limit)" : earliest.Value.ToString("yyyy-MM-dd")),-26} " +
            $"{availability.IsAvailable(recent, asOf),-10} {availability.IsAvailable(old, asOf)}");
    }
}

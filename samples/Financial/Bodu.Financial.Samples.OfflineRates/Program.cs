// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Samples.OfflineRates.Scenarios;

namespace Bodu.Financial.Samples.OfflineRates;

/// <summary>
/// Entry point for the offline-rates sample: builds a dated rate provider from a committed static data
/// file and exercises the lookup and money-conversion surfaces against it — no network required.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order against the offline provider.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.OfflineRates");
        Console.WriteLine("===================================");
        Console.WriteLine();

        // Offline source (default): illustrative AUD daily rates from a committed CSV.
        // FixedDatedRateProvider implements the same IDatedRateProvider / IRateProvider contracts
        // as every live web provider, so everything below works identically against a live feed.
        FixedDatedRateProvider source = StaticRates.LoadAudDaily();

        // --- To use the live Reserve Bank of Australia feed instead -----------------
        // 1. dotnet add package Bodu.Financial.ExchangeRates.Rba
        // 2. Replace the line above with:
        //
        //     using var rba = new RbaRateProvider(new RbaRateProviderOptions());
        //     await rba.LoadRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
        //     FixedDatedRateProvider source = rba.GetLoadedSnapshot();
        //
        //    (GetLoadedSnapshot() pins the fetched history as an immutable provider, so the
        //    scenarios below stay deterministic even after the live provider is disposed.)
        // ----------------------------------------------------------------------------

        LookupModes.Run(source);
        ConvertMoney.Run(source);

        Console.WriteLine("Done.");
    }
}

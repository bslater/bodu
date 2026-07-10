// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.CachedRates.Scenarios;

namespace Bodu.Financial.Samples.CachedRates;

/// <summary>
/// Entry point for the cached-rates sample: the read-through caching decorator, coverage-based range
/// serving, tiered cache stacking, and history-availability clamping — all against an offline source.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order against a fresh cache directory.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.CachedRates");
        Console.WriteLine("==================================");
        Console.WriteLine();

        // Each run starts from an empty cache directory so the hit/miss story is reproducible.
        var cacheDirectory = Path.Combine(AppContext.BaseDirectory, "rate-cache");
        if (Directory.Exists(cacheDirectory))
            Directory.Delete(cacheDirectory, recursive: true);

        // Offline source (default): a FixedDatedRateProvider over the committed CSV stands in for a
        // live feed. The caching decorator does not care - it wraps any IDatedRateProvider.
        //
        // --- To cache a live Reserve Bank of Australia feed instead -----------------
        // 1. dotnet add package Bodu.Financial.ExchangeRates.Rba
        // 2. Replace StaticRates.LoadAudDaily() below with:
        //
        //     using var source = new RbaRateProvider(new RbaRateProviderOptions());
        //
        //    (No preload needed - on a cache miss the decorator asks the provider, and the
        //    provider fetches from the RBA on demand.)
        // ----------------------------------------------------------------------------

        ReadThroughCache.Run(cacheDirectory);
        CoverageRanges.Run(cacheDirectory);
        TieredStacking.Run(cacheDirectory);
        HistoryClamping.Run();

        Console.WriteLine("Done.");
    }
}

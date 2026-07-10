// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TieredStacking.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.CachedRates.Scenarios;

/// <summary>
/// Demonstrates tiered cache stacking. A <see cref="CachingRateProvider" /> is itself an
/// <see cref="IDatedRateProvider" />, so caching layers compose like any other decorator:
/// a fast in-memory L1 over a durable file-backed L2 over the source. The same shape works with the
/// SQLite backend (<c>Bodu.Financial.ExchangeRates.Caching.Sqlite</c>) as L2 and a distributed cache
/// (<c>Bodu.Financial.ExchangeRates.Caching.Distributed</c>) shared across processes.
/// </summary>
public static class TieredStacking
{
    /// <summary>
    /// Builds a two-tier stack, then rebuilds L1 to show the durable L2 still shields the source.
    /// </summary>
    /// <param name="cacheDirectory">The directory used for file-backed caches in this run.</param>
    public static void Run(string cacheDirectory)
    {
        Console.WriteLine("--- Tiered stacking: in-memory L1 over durable file L2 ---");

        var source = new CountingRateProvider(StaticRates.LoadAudDaily());

        // L2: durable TOML file cache - survives process restarts.
        var l2Cache = new TomlFileRateCache(new FileRateCacheOptions
        {
            Provider = StaticRates.ProviderName,
            CacheDirectory = Path.Combine(cacheDirectory, "tiered"),
        });
        using var l2 = new CachingRateProvider(source, l2Cache, new CachingRateOptions());

        // L1: process-lifetime in-memory cache stacked on top of L2.
        using (var l1 = new CachingRateProvider(l2, new InMemoryRateCache(StaticRates.ProviderName), new CachingRateOptions()))
        {
            var date = new DateOnly(2024, 5, 20);

            // Miss everywhere: the call falls through L1 -> L2 -> source, and both tiers store it.
            l1.GetRate("AUD", "USD", date);
            Console.WriteLine($"Cold lookup     : source calls {source.CallCount} (filled L1 and L2)");

            // L1 hit: nothing below is touched.
            l1.GetRate("AUD", "USD", date);
            Console.WriteLine($"Warm lookup     : source calls {source.CallCount} (served by L1)");
        }

        // A "new process": fresh L1 memory, same durable L2 directory. The lookup misses L1 but is
        // served by L2 - the source still is not consulted again.
        using (var l1Rebuilt = new CachingRateProvider(l2, new InMemoryRateCache(StaticRates.ProviderName), new CachingRateOptions()))
        {
            RateLookupResult result = l1Rebuilt.GetRate("AUD", "USD", new DateOnly(2024, 5, 20));
            Console.WriteLine($"After 'restart' : source calls {source.CallCount} (served by L2, origin {result.Provenance.Origin})");
        }

        Console.WriteLine();
    }
}

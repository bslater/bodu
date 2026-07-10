// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadThroughCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.CachedRates.Scenarios;

/// <summary>
/// Demonstrates the read-through caching decorator: <see cref="CachingRateProvider" /> serves lookups
/// from an <see cref="IRateCache" /> and only consults the inner source on a miss. Every result carries
/// provenance saying which side served it.
/// </summary>
public static class ReadThroughCache
{
    /// <summary>
    /// Performs the same lookup twice and shows that only the first touches the source.
    /// </summary>
    /// <param name="cacheDirectory">The directory used for file-backed caches in this run.</param>
    public static void Run(string cacheDirectory)
    {
        Console.WriteLine("--- CachingRateProvider: read-through caching ---");

        // The counting wrapper stands in for "an expensive source" - with a live provider each
        // recorded call would be an HTTP fetch.
        var source = new CountingRateProvider(StaticRates.LoadAudDaily());

        // A TOML file cache persists rates per provider under the cache directory; swap in
        // new InMemoryRateCache(StaticRates.ProviderName) for a process-lifetime cache, or the
        // SQLite/distributed backends from the add-on packages for durable/shared caches.
        var cache = new TomlFileRateCache(new FileRateCacheOptions
        {
            Provider = StaticRates.ProviderName,
            CacheDirectory = cacheDirectory,
        });

        using var cached = new CachingRateProvider(source, cache, new CachingRateOptions
        {
            DefaultExpiry = TimeSpan.FromHours(24),
        });

        var date = new DateOnly(2024, 2, 14);

        // First lookup: cache miss -> the decorator asks the source, stores the result, serves it.
        RateLookupResult first = cached.GetRate("AUD", "USD", date);
        Print("1st lookup", first, source.CallCount);

        // Second lookup: served straight from the cache - the source is not consulted again.
        RateLookupResult second = cached.GetRate("AUD", "USD", date);
        Print("2nd lookup", second, source.CallCount);

        Console.WriteLine();
    }

    /// <summary>
    /// Prints one lookup with its provenance (origin, serving backend, cached-data age) and the
    /// cumulative number of calls that reached the source.
    /// </summary>
    /// <param name="label">The lookup label.</param>
    /// <param name="result">The result to describe.</param>
    /// <param name="sourceCalls">The cumulative source call count.</param>
    private static void Print(string label, RateLookupResult result, int sourceCalls)
    {
        RateProvenance provenance = result.Provenance;
        var served = provenance.Origin == RateOrigin.Cache
            ? $"cache ({provenance.Backend}, age {provenance.Age?.TotalSeconds:F0}s)"
            : "source (live)";
        Console.WriteLine($"{label}: {result.Rate.Rate} served from {served}; source calls so far: {sourceCalls}");
    }
}

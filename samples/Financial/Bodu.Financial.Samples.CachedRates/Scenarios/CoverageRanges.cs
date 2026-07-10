// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CoverageRanges.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.CachedRates.Scenarios;

/// <summary>
/// Demonstrates coverage-based range serving: the cache records which *date ranges* it has fetched,
/// not just which rows it holds. A range is served from the cache only when coverage contains the
/// whole window — a partially covered request refetches the full window rather than serving a sparse
/// row set — and an empty-but-fetched window (weekends, holidays) is covered knowledge that is not
/// refetched.
/// </summary>
public static class CoverageRanges
{
    /// <summary>
    /// Requests covered, partially covered, and empty ranges and shows what reaches the source.
    /// </summary>
    /// <param name="cacheDirectory">The directory used for file-backed caches in this run.</param>
    public static void Run(string cacheDirectory)
    {
        Console.WriteLine("--- Coverage-based range serving ---");

        var source = new CountingRateProvider(StaticRates.LoadAudDaily());
        var cache = new TomlFileRateCache(new FileRateCacheOptions
        {
            Provider = StaticRates.ProviderName,
            CacheDirectory = Path.Combine(cacheDirectory, "coverage"),
        });

        using var cached = new CachingRateProvider(source, cache, new CachingRateOptions());

        // First range: nothing is covered yet, so the window is fetched from the source once and the
        // rows and coverage are written back atomically.
        RateRangeResult march = cached.GetRates("AUD", "EUR", new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31));
        Console.WriteLine($"March          : {march.Count} observations; source calls: {source.CallCount}");

        // Same range again: coverage contains the whole window - served from the cache.
        march = cached.GetRates("AUD", "EUR", new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31));
        Console.WriteLine($"March again    : {march.Count} observations; source calls: {source.CallCount}");

        // Wider range: coverage does not contain April, so the request is a miss and the full window
        // is refetched. Serving March from cache and stitching April onto it is deliberately not done -
        // a sparse row set cannot be told apart from days that were never fetched, so coverage is
        // all-or-nothing per request window.
        RateRangeResult marchApril = cached.GetRates("AUD", "EUR", new DateOnly(2024, 3, 1), new DateOnly(2024, 4, 30));
        Console.WriteLine($"March..April   : {marchApril.Count} observations; source calls: {source.CallCount}");

        // A weekend inside the covered window is served from coverage: "no observation on these
        // days" is knowledge the cache already holds, so the source is not asked again.
        RateRangeResult covered = cached.GetRates("AUD", "EUR", new DateOnly(2024, 3, 23), new DateOnly(2024, 3, 24));
        Console.WriteLine($"Weekend (cov.) : {covered.Count} observations; source calls: {source.CallCount}");

        // Negative caching outside the covered window: an uncovered weekend-only range fetches once,
        // returns no rows, and records the coverage anyway - the second request is served from cache.
        RateRangeResult weekend = cached.GetRates("AUD", "EUR", new DateOnly(2024, 6, 8), new DateOnly(2024, 6, 9));
        weekend = cached.GetRates("AUD", "EUR", new DateOnly(2024, 6, 8), new DateOnly(2024, 6, 9));
        Console.WriteLine($"June wknd x2   : {weekend.Count} observations; source calls: {source.CallCount}");

        Console.WriteLine("Calls that reached the source:");
        foreach (var call in source.Calls)
            Console.WriteLine($"  {call}");

        Console.WriteLine();
    }
}

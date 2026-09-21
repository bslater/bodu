// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadThroughCaching.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Demonstrates the read-through decorator: <see cref="CachingNotableDateService" /> caches whole
/// (territory, civil-year) result lists, so repeated queries — including sub-range queries inside a cached
/// year — never re-run rule resolution.
/// </summary>
public static class ReadThroughCaching
{
    /// <summary>
    /// Resolves through an in-memory cache and counts how many queries reach the real engine.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Read-through caching over an in-memory backend",
            what: "Resolves a whole civil year through the caching decorator, repeats the same query, asks for a "
                + "sub-range inside that year, and runs a filtered query over it - counting how many of the four "
                + "reached the real engine.",
            why: "Resolving a year means running every rule in the pack, and the answer for a past year cannot "
                + "change. Caching whole civil years rather than individual queries is the design decision worth "
                + "noticing: a year is the unit rules are evaluated in, so it is the smallest thing worth "
                + "storing, and it means a sub-range query is a clip of something already computed rather than a "
                + "cache miss. Filters deliberately stay out of the cache key for the same reason - keying on "
                + "them would multiply entries for what is ultimately one result list filtered differently, and "
                + "two callers with different filters would each pay full price.",
            expect: "One engine resolution across all four queries. The count not moving is the whole assertion - "
                + "it is proved by call count rather than by timing, which is why the sample wraps the engine in "
                + "a counter instead of measuring elapsed milliseconds.");

        // The counting wrapper stands between the cache and the engine purely so the sample can
        // prove cache hits deterministically (by call count, not by timing).
        var engine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));

        using var cached = new CachingNotableDateService(
            engine,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions());

        // Cold: the whole civil year is resolved once and stored as one cache entry.
        var year = cached.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "AU");
        Console.WriteLine($"  Cold whole-year query: {year.Count} occurrences, engine resolutions = {engine.RangeResolutions}"
            + "  (the cold path: one engine resolution, stored as a single whole-year entry)");

        // Warm: the same year comes straight from the cache.
        _ = cached.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "AU");
        Console.WriteLine($"  Warm whole-year query: engine resolutions = {engine.RangeResolutions}"
            + "  (still 1 - the count not moving is the assertion, proved by call count rather than by timing)");

        // A sub-range inside a cached year is clipped from the cached list - still no engine work.
        var april = cached.Resolve(new DateRange(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)), "AU");
        Console.WriteLine($"  April sub-range from cache: {april.Count} occurrences, engine resolutions = {engine.RangeResolutions}"
            + "  (a clip of the cached year, not a miss - which is why a whole year is the right unit to store)");

        // Filters never enter the cache key: the filtered overload resolves the cached year, then filters.
        var holidays = cached.Resolve(
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            "AU",
            NotableDateFilter.IsNonWorkingDay());
        Console.WriteLine($"  Filtered query from cache: {holidays.Count} non-working dates, engine resolutions = {engine.RangeResolutions}"
            + "  (filters are applied after the cache, never keyed into it - otherwise two callers with different filters would each pay full price)");

        Console.WriteLine();
    }
}

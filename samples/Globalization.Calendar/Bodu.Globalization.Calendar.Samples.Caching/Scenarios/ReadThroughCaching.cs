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
        Console.WriteLine("--- CachingNotableDateService: read-through over InMemoryNotableDateCache ---");

        // The counting wrapper stands between the cache and the engine purely so the sample can
        // prove cache hits deterministically (by call count, not by timing).
        var engine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));

        using var cached = new CachingNotableDateService(
            engine,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions());

        // Cold: the whole civil year is resolved once and stored as one cache entry.
        var year = cached.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "AU");
        Console.WriteLine($"Cold whole-year query: {year.Count} occurrences, engine resolutions = {engine.RangeResolutions}");

        // Warm: the same year comes straight from the cache.
        _ = cached.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "AU");
        Console.WriteLine($"Warm whole-year query: engine resolutions = {engine.RangeResolutions}");

        // A sub-range inside a cached year is clipped from the cached list - still no engine work.
        var april = cached.Resolve(new DateRange(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)), "AU");
        Console.WriteLine($"April sub-range from cache: {april.Count} occurrences, engine resolutions = {engine.RangeResolutions}");

        // Filters never enter the cache key: the filtered overload resolves the cached year, then filters.
        var holidays = cached.Resolve(
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            "AU",
            NotableDateFilter.IsNonWorkingDay());
        Console.WriteLine($"Filtered query from cache: {holidays.Count} non-working dates, engine resolutions = {engine.RangeResolutions}");

        Console.WriteLine();
    }
}

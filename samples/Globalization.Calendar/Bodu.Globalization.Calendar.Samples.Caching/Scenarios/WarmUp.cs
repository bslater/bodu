// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WarmUp.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Demonstrates cache warm-up: pre-resolving the (territory, year) window the application will serve, so the
/// first real request never pays the rule-resolution cost.
/// </summary>
public static class WarmUp
{
    /// <summary>
    /// Warms two territories across two years, then shows every subsequent query hitting the cache.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CachingNotableDateService.Warm: pre-resolving the serving window ---");

        var engine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));
        using var cached = new CachingNotableDateService(
            engine,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions());

        // Warm the window the application serves: two territories, this year and next.
        int warmed = cached.Warm(new[] { "AU", "AU-VIC" }, firstYear: 2026, lastYear: 2027);
        Console.WriteLine($"Warmed {warmed} territories; engine resolutions during warm-up = {engine.RangeResolutions}");

        // Every query inside the warmed window is now a pure cache hit.
        _ = cached.Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)), "AU");
        _ = cached.Resolve(new DateRange(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31)), "AU-VIC");
        Console.WriteLine($"Two cross-window queries served; engine resolutions still = {engine.RangeResolutions}");

        // ------------------------------------------------------------------------------------------------
        // In a hosted application, register the warm-up as a background service instead of calling Warm
        // directly - AddNotableDateCacheWarmup runs after host start with a rolling year window:
        //
        //   builder.Services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
        //   builder.Services.AddCachedNotableDateService();
        //   builder.Services.AddNotableDateCacheWarmup(options =>
        //   {
        //       options.Territories = ["AU", "AU-VIC"];
        //       options.YearsBehind = 0;    // warm from the current year...
        //       options.YearsAhead = 1;     // ...through next year, recomputed each run
        //   });
        //
        // The hosted service no-ops (with a log message) when the registered INotableDateService is not
        // the caching decorator, so it is safe to leave registered in every environment.
        // ------------------------------------------------------------------------------------------------

        Console.WriteLine();
    }
}

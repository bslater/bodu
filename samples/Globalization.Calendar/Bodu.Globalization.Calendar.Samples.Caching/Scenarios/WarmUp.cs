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
        SampleConsole.Scenario(
            "Warming the cache - pre-resolving the serving window",
            what: "Pre-resolves two territories across two civil years, then runs two queries that each straddle "
                + "a year boundary, checking the engine counter before and after.",
            why: "A read-through cache moves the cost rather than removing it, and the caller who pays is "
                + "whoever asks first - which in a freshly deployed service is a real user, at the worst "
                + "possible moment. Warming pays it deliberately, at start-up, where the latency is nobody's "
                + "request. The window is (territory, year) pairs because that is the cache's unit, so warming "
                + "is exactly as granular as the cache is, with nothing wasted and no gaps. In a hosted "
                + "application the same thing runs as a background service with a rolling window, which the "
                + "comment at the end of this scenario sketches.",
            expect: "The warm-up does all the engine work up front. The two queries afterwards cross year "
                + "boundaries, so each touches two cached entries, and the engine counter still does not move - "
                + "every year they needed was already resolved.");

        var engine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));
        using var cached = new CachingNotableDateService(
            engine,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions());

        // Warm the window the application serves: two territories, this year and next.
        int warmed = cached.Warm(new[] { "AU", "AU-VIC" }, firstYear: 2026, lastYear: 2027);
        Console.WriteLine($"  Warmed {warmed} territories; engine resolutions during warm-up = {engine.RangeResolutions}"
            + "  (all the engine work, paid deliberately at start-up rather than by whoever asks first)");

        // Every query inside the warmed window is now a pure cache hit.
        _ = cached.Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)), "AU");
        _ = cached.Resolve(new DateRange(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31)), "AU-VIC");
        Console.WriteLine($"  Two cross-window queries served; engine resolutions still = {engine.RangeResolutions}"
            + "  (unchanged - both queries straddle a year boundary, so each touched two cached entries and found them)");

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

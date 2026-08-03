// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileBackedCaches.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Demonstrates the durable file backends: <see cref="JsonNotableDateCache" /> and
/// <see cref="TomlNotableDateCache" /> persist cached years to disk, so a fresh service instance — a new
/// process, in real deployments — starts warm from the previous run's files.
/// </summary>
public static class FileBackedCaches
{
    /// <summary>
    /// Persists a year through the JSON backend and serves a second service instance from the files.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- JsonNotableDateCache / TomlNotableDateCache: durable file backends ---");

        string cacheDirectory = Path.Combine(Path.GetTempPath(), "bodu-calendar-cache-sample");
        if (Directory.Exists(cacheDirectory))
            Directory.Delete(cacheDirectory, recursive: true);

        var options = new FileNotableDateCacheOptions { CacheDirectory = cacheDirectory };
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        // First "process": resolves once and writes the year to disk.
        var firstEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("NZ"));
        using (var first = new CachingNotableDateService(firstEngine, new JsonNotableDateCache(options), new NotableDateCachingOptions()))
        {
            _ = first.Resolve(range, "NZ");
        }

        Console.WriteLine($"First instance resolved from the engine: {firstEngine.RangeResolutions} resolution(s)");
        foreach (string file in Directory.EnumerateFiles(cacheDirectory, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            Console.WriteLine($"  cache file: {Path.GetFileName(file)}");

        // Second "process": a brand-new service over the same directory starts warm - the engine is never hit.
        var secondEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("NZ"));
        using (var second = new CachingNotableDateService(secondEngine, new JsonNotableDateCache(options), new NotableDateCachingOptions()))
        {
            var occurrences = second.Resolve(range, "NZ");
            Console.WriteLine($"Second instance served {occurrences.Count} occurrences with {secondEngine.RangeResolutions} engine resolution(s)");
        }

        // The TOML backend is a drop-in swap when human-readable/diffable cache files are preferred.
        var tomlEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("NZ"));
        string tomlDirectory = Path.Combine(cacheDirectory, "toml");
        using (var toml = new CachingNotableDateService(tomlEngine, new TomlNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = tomlDirectory }), new NotableDateCachingOptions()))
        {
            _ = toml.Resolve(range, "NZ");
        }

        foreach (string file in Directory.EnumerateFiles(tomlDirectory).OrderBy(f => f, StringComparer.Ordinal))
            Console.WriteLine($"  toml cache file: {Path.GetFileName(file)}");

        Directory.Delete(cacheDirectory, recursive: true);
        Console.WriteLine();
    }
}

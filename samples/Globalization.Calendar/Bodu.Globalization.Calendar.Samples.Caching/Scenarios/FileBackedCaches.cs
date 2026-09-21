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
        SampleConsole.Scenario(
            "Durable file backends - starting warm in a new process",
            what: "Resolves a year through a JSON-file cache and lists the files written, then builds a "
                + "completely fresh service over the same directory and resolves the same range, counting engine "
                + "work each time - and finally writes the same data through the TOML backend.",
            why: "An in-memory cache is empty at every process start, which is exactly when a service is least "
                + "able to absorb the work - a deployment restarts every instance at once, and each one "
                + "re-resolves every year it serves. A file-backed cache survives the restart, so the cost is "
                + "paid once per data version rather than once per process. Two file formats ship because the "
                + "choice is about who reads the file: JSON is the default, while TOML is worth having when the "
                + "cache is committed or inspected, since it diffs legibly. The second service instance here "
                + "stands in for the new process, which is what makes the claim testable offline.",
            expect: "The first instance does the engine work and leaves files behind. The second - a brand-new "
                + "service with its own counter - serves the same range with zero engine resolutions, having "
                + "read only what the first wrote. The TOML backend is a constructor swap, nothing more.");

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

        Console.WriteLine($"  First instance resolved from the engine: {firstEngine.RangeResolutions} resolution(s)"
            + "  (the cold path, and the only time the rules run)");
        foreach (string file in Directory.EnumerateFiles(cacheDirectory, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            Console.WriteLine($"    cache file: {Path.GetFileName(file)}");

        Console.WriteLine("  (one file per territory, holding its cached years - this is what survives the process exit)");

        // Second "process": a brand-new service over the same directory starts warm - the engine is never hit.
        var secondEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("NZ"));
        using (var second = new CachingNotableDateService(secondEngine, new JsonNotableDateCache(options), new NotableDateCachingOptions()))
        {
            var occurrences = second.Resolve(range, "NZ");
            Console.WriteLine($"  Second instance served {occurrences.Count} occurrences with {secondEngine.RangeResolutions} engine resolution(s)"
                + "  (expected 0 - a brand-new service with its own counter, standing in for a new process, starts warm)");
        }

        // The TOML backend is a drop-in swap when human-readable/diffable cache files are preferred.
        var tomlEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("NZ"));
        string tomlDirectory = Path.Combine(cacheDirectory, "toml");
        using (var toml = new CachingNotableDateService(tomlEngine, new TomlNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = tomlDirectory }), new NotableDateCachingOptions()))
        {
            _ = toml.Resolve(range, "NZ");
        }

        foreach (string file in Directory.EnumerateFiles(tomlDirectory).OrderBy(f => f, StringComparer.Ordinal))
            Console.WriteLine($"    toml cache file: {Path.GetFileName(file)}");

        Console.WriteLine("  (a constructor swap and nothing else - worth it when the cache is committed or inspected, since TOML diffs legibly)");

        Directory.Delete(cacheDirectory, recursive: true);
        Console.WriteLine();
    }
}

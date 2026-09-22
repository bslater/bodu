// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DurableBackends.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Demonstrates the two add-on cache backends — <c>Bodu.Globalization.Calendar.Caching.Sqlite</c> and
/// <c>…Caching.Distributed</c> — behind the same <see cref="INotableDateCache" /> contract the in-memory and file
/// backends implement.
/// </summary>
public static class DurableBackends
{
    /// <summary>
    /// Runs the same warm/cold sequence through the SQLite backend and an in-process distributed cache, showing that
    /// both survive the service being disposed and rebuilt.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Durable backends - SQLite and IDistributedCache",
            what: "Resolves a year through a SQLite-backed cache, disposes the service, builds a second one over the "
                + "same database file, and repeats the query - then does the same over an IDistributedCache. Engine "
                + "resolutions are counted throughout.",
            why: "The in-memory cache dies with the process, which is the wrong shape for two common deployments. A "
                + "CLI or a serverless function starts cold every time, so it wants a file it can carry between "
                + "runs - that is SQLite. A web farm has many processes that should not each recompute the same "
                + "year, so it wants one shared store - that is IDistributedCache, with Redis as the usual "
                + "implementation. Both sit behind the same INotableDateCache contract, so choosing between them "
                + "is a composition decision at startup rather than a change to any calling code.",
            expect: "One engine resolution per backend, not two. The second service reads the year the first one "
                + "wrote - from the database file after a dispose, and from the shared store - which is the whole "
                + "claim these backends make. The sample uses MemoryDistributedCache so it runs offline; in "
                + "production the same code takes a Redis-backed IDistributedCache instead.");

        var year = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        // --- SQLite: durable across process restarts -------------------------------------------------
        var databaseFile = Path.Combine(Path.GetTempPath(), $"bodu-calendar-cache-{Guid.NewGuid():N}.db");
        try
        {
            var sqliteEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));

            // First service: cold, so the year is computed and written to the database file.
            using (var first = new CachingNotableDateService(
                sqliteEngine,
                new SqliteNotableDateCache(databaseFile),
                new NotableDateCachingOptions()))
            {
                var resolved = first.Resolve(year, "AU");
                Console.WriteLine($"  SQLite  cold  : {resolved.Count,3} occurrences, engine resolutions = {sqliteEngine.RangeResolutions}"
                    + "   (computed once and written to the database file)");
            }

            // Second service over the SAME file, with the first one already disposed: the year survives.
            using (var second = new CachingNotableDateService(
                sqliteEngine,
                new SqliteNotableDateCache(databaseFile),
                new NotableDateCachingOptions()))
            {
                var resolved = second.Resolve(year, "AU");
                Console.WriteLine($"  SQLite  warm  : {resolved.Count,3} occurrences, engine resolutions = {sqliteEngine.RangeResolutions}"
                    + "   (a NEW service over the same file - the count did not move, so this came off disk)");
            }

            Console.WriteLine($"  database file : {new FileInfo(databaseFile).Length} bytes on disk   (what a cold-start CLI or function carries between runs)");
        }
        finally
        {
            // Samples clean up after themselves; a real deployment keeps the file.
            if (File.Exists(databaseFile))
                File.Delete(databaseFile);
        }

        Console.WriteLine();

        // --- IDistributedCache: shared between processes ---------------------------------------------
        // MemoryDistributedCache is an in-process implementation of the same contract Redis implements,
        // which is what lets this scenario demonstrate the backend without a server.
        IDistributedCache shared = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var distributedEngine = new CountingNotableDateService(AsiaPacificCalendarData.CreateService("AU"));
        var distributedOptions = new DistributedNotableDateCacheOptions { KeyPrefix = "sample:calendar:" };

        // Two independently constructed services standing in for two processes on the same store.
        using (var nodeA = new CachingNotableDateService(
            distributedEngine,
            new DistributedNotableDateCache(shared, distributedOptions),
            new NotableDateCachingOptions()))
        {
            var resolved = nodeA.Resolve(year, "AU");
            Console.WriteLine($"  shared  node A: {resolved.Count,3} occurrences, engine resolutions = {distributedEngine.RangeResolutions}"
                + "   (first node through: computed and published to the shared store)");
        }

        using (var nodeB = new CachingNotableDateService(
            distributedEngine,
            new DistributedNotableDateCache(shared, distributedOptions),
            new NotableDateCachingOptions()))
        {
            var resolved = nodeB.Resolve(year, "AU");
            Console.WriteLine($"  shared  node B: {resolved.Count,3} occurrences, engine resolutions = {distributedEngine.RangeResolutions}"
                + "   (a second node - it read what node A published rather than recomputing)");
        }

        Console.WriteLine("  (swap MemoryDistributedCache for a Redis-backed IDistributedCache and nothing above changes)");
        Console.WriteLine();
    }
}

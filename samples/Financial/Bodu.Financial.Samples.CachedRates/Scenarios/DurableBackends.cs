// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DurableBackends.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.Samples.CachedRates.Scenarios;

/// <summary>
/// Demonstrates the two add-on rate-cache backends — <c>Bodu.Financial.ExchangeRates.Caching.Sqlite</c> and
/// <c>…Caching.Distributed</c> — behind the same <see cref="IRateCache" /> contract the in-memory and TOML backends
/// implement.
/// </summary>
public static class DurableBackends
{
    /// <summary>
    /// Runs the same lookup through a SQLite-backed cache across two provider instances, then through an
    /// <see cref="IDistributedCache" /> shared by two instances standing in for two processes.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Durable rate caches - SQLite and IDistributedCache",
            what: "Looks a rate up through a SQLite-backed cache, disposes the provider, builds a second one over "
                + "the same database file, and repeats the lookup - then does the same over an IDistributedCache. "
                + "Calls that reach the source are counted throughout.",
            why: "A rate fetch is an HTTP request against someone else's service, often a metered one, and the "
                + "answer for a past date never changes. An in-memory cache loses that on every restart, so a "
                + "scheduled job re-fetches history it already had, and a web farm multiplies the bill by the "
                + "number of instances. SQLite keeps the rows in a file the next run reopens; IDistributedCache "
                + "puts them where every instance sees them. Both implement IRateCache, so which one a deployment "
                + "uses is a composition choice and nothing in the calling code moves.",
            expect: "One source call per backend. The second provider answers from what the first stored - off disk "
                + "after a dispose, and out of the shared store for the second node - and every result still "
                + "carries its provenance, so a cached answer is not mistaken for a freshly fetched one. "
                + "MemoryDistributedCache keeps the sample offline; a Redis-backed IDistributedCache is the same "
                + "code.");

        var date = new DateOnly(2024, 2, 14);

        // --- SQLite: durable across process restarts -------------------------------------------------
        var databaseFile = Path.Combine(Path.GetTempPath(), $"bodu-rate-cache-{Guid.NewGuid():N}.db");
        try
        {
            var sqliteSource = new CountingRateProvider(StaticRates.LoadAudDaily());

            using (var first = new CachingRateProvider(
                sqliteSource,
                new SqliteRateCache(StaticRates.ProviderName, databaseFile),
                new CachingRateOptions()))
            {
                var result = first.GetRate("AUD", "USD", date);
                Console.WriteLine($"  SQLite  cold  : AUD/USD {result.Rate.Rate,-10:0.######} source calls = {sqliteSource.CallCount}"
                    + "   (fetched once and written to the database file)");
            }

            // A second provider over the same file, with the first disposed: the row is still there.
            using (var second = new CachingRateProvider(
                sqliteSource,
                new SqliteRateCache(StaticRates.ProviderName, databaseFile),
                new CachingRateOptions()))
            {
                var result = second.GetRate("AUD", "USD", date);
                Console.WriteLine($"  SQLite  warm  : AUD/USD {result.Rate.Rate,-10:0.######} source calls = {sqliteSource.CallCount}"
                    + "   (a NEW provider over the same file - the count did not move, so no request was made)");
            }

            Console.WriteLine($"  database file : {new FileInfo(databaseFile).Length} bytes on disk   (what a scheduled job carries between runs instead of re-fetching)");
        }
        finally
        {
            if (File.Exists(databaseFile))
                File.Delete(databaseFile);
        }

        Console.WriteLine();

        // --- IDistributedCache: shared between processes ---------------------------------------------
        // MemoryDistributedCache implements the same contract Redis does, so the scenario needs no server.
        IDistributedCache shared = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var distributedSource = new CountingRateProvider(StaticRates.LoadAudDaily());

        using (var nodeA = new CachingRateProvider(
            distributedSource,
            new DistributedRateCache(shared, StaticRates.ProviderName),
            new CachingRateOptions()))
        {
            var result = nodeA.GetRate("AUD", "USD", date);
            Console.WriteLine($"  shared  node A: AUD/USD {result.Rate.Rate,-10:0.######} source calls = {distributedSource.CallCount}"
                + "   (first node through: fetched once and published to the shared store)");
        }

        using (var nodeB = new CachingRateProvider(
            distributedSource,
            new DistributedRateCache(shared, StaticRates.ProviderName),
            new CachingRateOptions()))
        {
            var result = nodeB.GetRate("AUD", "USD", date);
            Console.WriteLine($"  shared  node B: AUD/USD {result.Rate.Rate,-10:0.######} source calls = {distributedSource.CallCount}"
                + $"   (a second node - it read node A's row; provenance still reports Origin={result.Provenance.Origin}, Backend={result.Provenance.Backend}, so a cached answer is never mistaken for a fresh fetch)");
        }

        Console.WriteLine("  (swap MemoryDistributedCache for a Redis-backed IDistributedCache and nothing above changes)");
        Console.WriteLine();
    }
}

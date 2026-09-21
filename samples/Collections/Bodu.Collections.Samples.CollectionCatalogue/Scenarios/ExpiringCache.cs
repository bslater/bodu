// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExpiringCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates the time dimension of <see cref="EvictingDictionary{TKey, TValue}" />: supplying an
/// <see cref="EvictingDictionaryExpiration" /> gives entries a time-to-live that is independent of the
/// capacity-triggered <see cref="EvictingDictionaryPolicy" />, measured either
/// <see cref="EvictingDictionaryExpirationKind.Absolute" />ly or on a
/// <see cref="EvictingDictionaryExpirationKind.Sliding" /> window.
/// </summary>
public static class ExpiringCache
{
    /// <summary>A fixed start instant so the printed clock readings are stable.</summary>
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Runs the absolute and sliding expiration walkthroughs against a manually advanced clock.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- EvictingDictionary<TKey, TValue>: time-based expiration ---");

        RunAbsolute();
        RunSliding();
        RunPerEntryTimeToLive();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows an absolute time-to-live: reads do not extend an entry's life.
    /// </summary>
    private static void RunAbsolute()
    {
        Console.WriteLine("  Absolute (reads do not extend the lifetime):");

        var clock = new ManualTimeProvider(Start);

        // TimeToLive is the default lifetime for entries added without a per-entry override. Passing the
        // TimeProvider is what makes this demonstrable - see ManualTimeProvider for why.
        var expiration = new EvictingDictionaryExpiration(
            TimeSpan.FromMinutes(10),
            EvictingDictionaryExpirationKind.Absolute,
            clock);

        var cache = new EvictingDictionary<string, string>(capacity: 8, expiration);
        cache["session-a"] = "user-1";
        cache["session-b"] = "user-2";   // added at the same instant, but never read again

        // Eight minutes in, the entries are still live and a read returns one.
        clock.Advance(TimeSpan.FromMinutes(8));
        Console.WriteLine($"    +08m read a  : {Show(cache, "session-a")}");

        // Under Absolute the countdown started when the entry was added, so that read bought it nothing. Four
        // minutes later both entries are twelve minutes old and past their ten-minute life.
        clock.Advance(TimeSpan.FromMinutes(4));
        Console.WriteLine($"    +12m read a  : {Show(cache, "session-a")}");

        // There is no background timer. An access that lands on an expired key removes it there and then, so the
        // read above already reclaimed session-a - but session-b was never touched, so it still occupies a slot and
        // still counts towards Count even though no lookup can see it.
        // Count reports stored entries, so session-b still shows up there, while enumeration presents only live
        // entries. The two disagreeing is the signal that a sweep is due.
        Console.WriteLine($"    Count        : {cache.Count} (session-b lingers - expired but never accessed)");
        // Note the Select: LINQ's Count() would short-circuit to the ICollection Count property above rather than
        // walking the entries, and would report the stored count instead of the live one.
        Console.WriteLine($"    enumerated   : {cache.Select(pair => pair.Key).Count()} live entries (expired entries are skipped)");

        // RemoveExpired() is the explicit sweep that reconciles Count with the live set. Call it periodically for a
        // cache that can sit idle; each removal raises ItemEvicted exactly as a capacity eviction would.
        Console.WriteLine($"    RemoveExpired: {cache.RemoveExpired()} removed, Count now {cache.Count}");
    }

    /// <summary>
    /// Shows a sliding time-to-live: every successful read restarts the countdown.
    /// </summary>
    private static void RunSliding()
    {
        Console.WriteLine("  Sliding (each read restarts the countdown):");

        var clock = new ManualTimeProvider(Start);
        var cache = new EvictingDictionary<string, string>(
            capacity: 8,
            new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, clock));

        cache["session-b"] = "user-2";

        // Three reads, eight minutes apart. Each one is within the window and pushes the deadline out again, so an
        // actively used session survives twenty-four minutes on a ten-minute TTL.
        for (var step = 1; step <= 3; step++)
        {
            clock.Advance(TimeSpan.FromMinutes(8));
            Console.WriteLine($"    +{step * 8:00}m read    : {Show(cache, "session-b")}");
        }

        // Stop reading and the window finally closes.
        clock.Advance(TimeSpan.FromMinutes(11));
        Console.WriteLine($"    +35m read    : {Show(cache, "session-b")} (idle for 11m)");

        // Touch() promotes an entry for the *capacity* policy but deliberately does not refresh its lifetime -
        // recency and freshness are separate dimensions, so a touch cannot resurrect stale data.
        Console.WriteLine($"    Touch()      : {cache.Touch("session-b")} (nothing to promote - it is gone)");
    }

    /// <summary>
    /// Shows a per-entry time-to-live overriding the configured default.
    /// </summary>
    private static void RunPerEntryTimeToLive()
    {
        Console.WriteLine("  Per-entry TTL overriding the default:");

        var clock = new ManualTimeProvider(Start);

        // A null default TimeToLive means entries added through Add or the indexer never expire; only entries added
        // through the TTL overloads carry a lifetime. That is the mixed cache: permanent config, expiring tokens.
        var cache = new EvictingDictionary<string, string>(
            capacity: 8,
            new EvictingDictionaryExpiration(timeToLive: null, EvictingDictionaryExpirationKind.Absolute, clock));

        cache["region"] = "ap-southeast-2";                             // no lifetime
        cache.Add("token", "abc123", TimeSpan.FromMinutes(5));          // five-minute lifetime

        clock.Advance(TimeSpan.FromMinutes(6));
        Console.WriteLine($"    +06m region  : {Show(cache, "region")} (added without a TTL)");
        Console.WriteLine($"    +06m token   : {Show(cache, "token")} (5m TTL elapsed)");
        Console.WriteLine($"    survivors    : {string.Join(", ", cache.Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal))}");
    }

    /// <summary>
    /// Reads a key through <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" /> and renders the outcome.
    /// </summary>
    /// <param name="cache">The cache to read.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The stored value, or a marker showing the entry is no longer visible.</returns>
    private static string Show(EvictingDictionary<string, string> cache, string key) =>
        cache.TryGetValue(key, out var value) ? $"hit  ({value})" : "miss (expired)";
}

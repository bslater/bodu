// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates <see cref="EvictingDictionary{TKey, TValue}" /> as a bounded cache: once the capacity is
/// reached, inserting a new key evicts an existing one chosen by the configured
/// <see cref="EvictingDictionaryPolicy" />. This scenario uses the least-recently-used policy so a read
/// (via the indexer) protects a key from eviction.
/// </summary>
/// <remarks>
/// The detail that catches people is that a <em>read</em> mutates eviction order under LRU. The indexer looks like
/// a pure lookup and is not: it is what keeps a hot key alive. That cuts both ways — a diagnostic peek at a cache
/// entry silently promotes it, which is exactly why <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" />
/// exists as a separate, non-promoting call.
/// </remarks>
public static class EvictingCache
{
    /// <summary>
    /// Fills an LRU cache to capacity, touches one key to promote it, then inserts to force an eviction.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "EvictingDictionary<TKey,TValue> - bounded LRU cache",
            what: "Fills a capacity-3 LRU cache, reads one key back to promote it, asks which key would go next, " +
                  "then inserts a fourth key to force the eviction.",
            why: "A bounded cache has to choose a victim, and the policy is that choice. Least-recently-used bets " +
                 "that what you touched lately you will touch again - so under LRU a read is not a passive " +
                 "operation, it is how a key earns its place. That surprises people twice: the hot key survives " +
                 "because reads protect it, and an innocent-looking diagnostic read reorders the cache. " +
                 "PeekEvictionCandidate is the way to ask the question without changing the answer.",
            expect: "After reading alpha the recency order is beta, gamma, alpha - so beta is named as the next " +
                    "victim and beta is what the fourth insert actually evicts. alpha survives purely because it " +
                    "was read, despite having been inserted first.");

        // Capacity 3, least-recently-used: the eviction victim is always the key untouched for longest.
        var cache = new EvictingDictionary<string, int>(capacity: 3, EvictingDictionaryPolicy.LeastRecentlyUsed);

        // ItemEvicted fires after a key is removed to make room - it records the eviction order deterministically.
        cache.ItemEvicted += (key, value) => Console.WriteLine($"  evicted: {key}={value}  (expected beta=2 - fires after the removal commits, so a handler always sees a consistent cache)");

        // Insert three keys; recency order (oldest -> newest) is now: alpha, beta, gamma.
        cache["alpha"] = 1;
        cache["beta"] = 2;
        cache["gamma"] = 3;
        Console.WriteLine($"  policy       : {cache.Policy}  (the victim-selection rule; six are available, and this is the one where reads matter)");

        // Read "alpha" through the indexer: an LRU access promotes it to most-recently-used.
        // Recency order becomes: beta, gamma, alpha.
        var _ = cache["alpha"];
        Console.WriteLine("  touched alpha via read (now most-recently-used)  - the indexer is not a pure read under LRU");

        // PeekEvictionCandidate reports the next victim without mutating recency - it is "beta".
        Console.WriteLine($"  next victim  : {cache.PeekEvictionCandidate()}  (expected beta - alpha was just promoted past it; asking this way does not itself promote anything)");

        // Inserting a fourth key exceeds capacity 3 and evicts the least-recently-used key: "beta".
        cache["delta"] = 4;

        // Print the survivors sorted by key so the output is stable regardless of internal order.
        var survivors = cache.OrderBy(pair => pair.Key, StringComparer.Ordinal);
        Console.WriteLine($"  survivors    : {string.Join(", ", survivors.Select(p => $"{p.Key}={p.Value}"))}  (expected alpha, delta, gamma - alpha outlived beta only because it was read; printed key-sorted for a stable transcript)");

        Console.WriteLine();
    }
}

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
public static class EvictingCache
{
    /// <summary>
    /// Fills an LRU cache to capacity, touches one key to promote it, then inserts to force an eviction.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- EvictingDictionary<TKey, TValue>: bounded LRU cache ---");

        // Capacity 3, least-recently-used: the eviction victim is always the key untouched for longest.
        var cache = new EvictingDictionary<string, int>(capacity: 3, EvictingDictionaryPolicy.LeastRecentlyUsed);

        // ItemEvicted fires after a key is removed to make room - it records the eviction order deterministically.
        cache.ItemEvicted += (key, value) => Console.WriteLine($"  evicted: {key}={value}");

        // Insert three keys; recency order (oldest -> newest) is now: alpha, beta, gamma.
        cache["alpha"] = 1;
        cache["beta"] = 2;
        cache["gamma"] = 3;
        Console.WriteLine($"  policy       : {cache.Policy}");

        // Read "alpha" through the indexer: an LRU access promotes it to most-recently-used.
        // Recency order becomes: beta, gamma, alpha.
        var _ = cache["alpha"];
        Console.WriteLine("  touched alpha via read (now most-recently-used)");

        // PeekEvictionCandidate reports the next victim without mutating recency - it is "beta".
        Console.WriteLine($"  next victim  : {cache.PeekEvictionCandidate()}");

        // Inserting a fourth key exceeds capacity 3 and evicts the least-recently-used key: "beta".
        cache["delta"] = 4;

        // Print the survivors sorted by key so the output is stable regardless of internal order.
        var survivors = cache.OrderBy(pair => pair.Key, StringComparer.Ordinal);
        Console.WriteLine($"  survivors    : {string.Join(", ", survivors.Select(p => $"{p.Key}={p.Value}"))}");

        Console.WriteLine();
    }
}

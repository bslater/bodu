// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFlightCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;
using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> as a bounded cache: the
/// single-flight <c>GetOrAdd(key, factory)</c> that runs a value factory at most once per key, and the
/// <c>ItemEvicted</c> callback that fires when capacity pressure forces an entry out. Everything runs
/// single-threaded so the counts, order, and totals are deterministic.
/// </summary>
public static class SingleFlightCache
{
    /// <summary>
    /// Shows the factory running once per key, the first-in-first-out eviction order at capacity, and
    /// the eviction accounting invariant on a larger overflow.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ConcurrentEvictingDictionary<TKey,TValue>: single-flight cache ---");

        // Part 1: single-flight GetOrAdd. The factory is invoked inside the owning segment's lock, so a
        // repeated key is served from the stored value and never recomputes. The counter is the evidence.
        var factoryCalls = 0;
        var cache = new ConcurrentEvictingDictionary<int, string>(capacity: 8, EvictingDictionaryPolicy.FirstInFirstOut);

        for (var i = 0; i < 5; i++)
            _ = cache.GetOrAdd(42, key =>
            {
                factoryCalls++;                     // runs at most once for key 42
                return $"value-of-{key}";
            });

        Console.WriteLine($"GetOrAdd(42) x5   : factory invoked {factoryCalls} time(s)");

        Console.WriteLine();

        // Part 2: eviction order. With capacity 1 the dictionary owns a single segment, so first-in,
        // first-out eviction is exact and observable: each new distinct key displaces the resident, and
        // ItemEvicted observes the displaced keys in arrival order.
        var order = new List<int>();
        var single = new ConcurrentEvictingDictionary<int, string>(capacity: 1, EvictingDictionaryPolicy.FirstInFirstOut);
        single.ItemEvicted += (key, _) => order.Add(key);

        foreach (var key in new[] { 1, 2, 3, 4 })
            single.Add(key, $"v{key}");

        Console.WriteLine($"cap 1, add 1..4   : evicted in order [{string.Join(", ", order)}], resident {single.ToArray()[0].Key}");

        Console.WriteLine();

        // Part 3: eviction accounting under overflow. Insert 20 distinct keys into a capacity-8 cache.
        // The live count can never exceed the capacity, and every inserted key is either still resident
        // or was evicted exactly once, so (survivors + evictions) always equals the number inserted -
        // an order-independent invariant that holds regardless of how keys route across segments.
        var eventFires = 0;
        var bounded = new ConcurrentEvictingDictionary<int, int>(capacity: 8, EvictingDictionaryPolicy.FirstInFirstOut);
        bounded.ItemEvicted += (_, _) => Interlocked.Increment(ref eventFires);

        const int inserted = 20;
        for (var key = 0; key < inserted; key++)
            bounded.Add(key, key);

        var survivors = bounded.Count;
        var evictions = bounded.EvictionCount;

        Console.WriteLine($"cap 8, add 20     : survivors {survivors}, evictions {evictions}");
        Console.WriteLine($"accounting        : survivors + evictions == inserted -> {survivors + evictions == inserted}");
        Console.WriteLine($"event fires match : ItemEvicted fired == EvictionCount -> {eventFires == evictions}");

        Console.WriteLine();
    }
}

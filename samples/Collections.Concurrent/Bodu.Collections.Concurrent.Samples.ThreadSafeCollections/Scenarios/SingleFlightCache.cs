// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFlightCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;
using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> as a bounded cache: the single-flight
/// <c>GetOrAdd(key, factory)</c> that runs a value factory at most once per key, and the <c>ItemEvicted</c> callback
/// that reports what capacity pressure pushed out.
/// </summary>
/// <remarks>
/// This scenario is single-threaded so the counts and the eviction order are exact and reproducible; the
/// <c>ParallelSafety</c> scenario shows the same single-flight guarantee holding under a genuine stampede.
/// </remarks>
public static class SingleFlightCache
{
    /// <summary>
    /// Shows the factory running once per key, the first-in-first-out eviction order at capacity, and the eviction
    /// accounting invariant that survives an overflow of more than twice the capacity.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ConcurrentEvictingDictionary<TKey,TValue> - single-flight bounded cache",
            what: "Calls GetOrAdd for one key five times while counting factory invocations, watches a capacity-1 " +
                  "cache report each eviction as it happens, then overflows a capacity-8 cache with 20 keys and " +
                  "checks the books balance.",
            why: "A cache without single-flight turns a miss into a thundering herd: every caller that misses runs " +
                 "the expensive load, so the moment an entry expires the backing store takes N identical queries " +
                 "instead of one. Running the factory inside the owning segment's lock collapses those to one. The " +
                 "ItemEvicted callback matters for the opposite reason - a bounded cache discards silently by " +
                 "design, and the callback is the only way to learn what it dropped.",
            expect: "The factory runs exactly once for five GetOrAdd calls. The capacity-1 cache evicts 1, 2, 3 in " +
                    "arrival order and holds 4. After 20 inserts into 8 slots, survivors + evictions == 20 and the " +
                    "callback fired exactly EvictionCount times - both True.");

        // Part 1: single-flight GetOrAdd. The factory is invoked inside the owning segment's lock, so a repeated key
        // is served from the stored value and never recomputes. The counter is the evidence: if the factory were
        // called per lookup this would print 5.
        var factoryCalls = 0;
        var cache = new ConcurrentEvictingDictionary<int, string>(capacity: 8, EvictingDictionaryPolicy.FirstInFirstOut);

        for (var i = 0; i < 5; i++)
            _ = cache.GetOrAdd(42, key =>
            {
                factoryCalls++;                     // runs at most once for key 42
                return $"value-of-{key}";
            });

        Console.WriteLine("  Single-flight GetOrAdd - five lookups of one cold key:");
        Console.WriteLine($"    factory calls  : {factoryCalls}  (expected 1 - four lookups were served from the stored value)");
        Console.WriteLine();

        // Part 2: eviction order. With capacity 1 the dictionary owns a single segment, so first-in-first-out
        // eviction is exact and observable: each new distinct key displaces the resident, and ItemEvicted reports
        // the displaced keys in arrival order. At larger capacities keys route across segments and the global order
        // is no longer meaningful - which is why this part uses capacity 1 rather than asserting an order it cannot
        // promise.
        var order = new List<int>();
        var single = new ConcurrentEvictingDictionary<int, string>(capacity: 1, EvictingDictionaryPolicy.FirstInFirstOut);
        single.ItemEvicted += (key, _) => order.Add(key);

        foreach (var key in new[] { 1, 2, 3, 4 })
            single.Add(key, $"v{key}");

        Console.WriteLine("  Eviction order at capacity 1 (FIFO, so oldest goes first):");
        Console.WriteLine($"    evicted        : [{string.Join(", ", order)}]  (expected [1, 2, 3] - each displaced by its successor)");
        Console.WriteLine($"    resident       : {single.ToArray()[0].Key}  (expected 4 - the last one in is the only survivor)");
        Console.WriteLine();

        // Part 3: eviction accounting under overflow. Twenty distinct keys go into eight slots. The live count can
        // never exceed the capacity, and every inserted key is either still resident or was evicted exactly once, so
        // (survivors + evictions) always equals the number inserted. That identity is order-independent: it holds
        // however keys happen to route across segments, which is what makes it worth asserting when the per-key
        // outcome is not predictable.
        var eventFires = 0;
        var bounded = new ConcurrentEvictingDictionary<int, int>(capacity: 8, EvictingDictionaryPolicy.FirstInFirstOut);
        bounded.ItemEvicted += (_, _) => Interlocked.Increment(ref eventFires);

        const int inserted = 20;
        for (var key = 0; key < inserted; key++)
            bounded.Add(key, key);

        var survivors = bounded.Count;
        var evictions = bounded.EvictionCount;

        Console.WriteLine("  Accounting after 20 inserts into 8 slots:");
        Console.WriteLine($"    survivors      : {survivors}  (never exceeds the capacity of 8 - that is what bounded means)");
        Console.WriteLine($"    evictions      : {evictions}  (EvictionCount, the cache's own tally)");
        Console.WriteLine($"    books balance  : {survivors + evictions == inserted}  (expected True - every key is resident or evicted exactly once, never both and never neither)");
        Console.WriteLine($"    callback count : {eventFires == evictions}  (expected True - ItemEvicted fired once per eviction, so no drop went unreported)");

        Console.WriteLine();
    }
}

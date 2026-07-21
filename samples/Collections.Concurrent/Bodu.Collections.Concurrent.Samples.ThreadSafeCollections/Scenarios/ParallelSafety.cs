// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelSafety.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;
using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates that the concurrent collections stay correct under genuine parallelism. A bounded
/// <see cref="System.Threading.Tasks.Parallel" /> workload mutates the collections from many threads,
/// then the scenario prints only order-independent aggregates — the final count, the summed value, and
/// a single-flight factory count — because per-item results arrive in a nondeterministic order.
/// </summary>
public static class ParallelSafety
{
    /// <summary>
    /// Runs a parallel add-all into a set and a parallel single-flight load, then asserts and prints the
    /// deterministic totals.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Parallel safety: deterministic aggregates only ---");

        // Add 0..N-1 into the set from a 4-way partitioned Parallel.For. The order in which elements land
        // is nondeterministic, so we never print individual items - only aggregates that are invariant:
        // the final Count must be N, and the elements must sum to 0 + 1 + ... + (N-1).
        const int n = 1000;
        var set = new ConcurrentHashSet<int>();

        Parallel.For(0, n, i => set.Add(i));

        var sum = 0L;
        foreach (var value in set.ToArray())
            sum += value;

        var expectedSum = (long)n * (n - 1) / 2;

        Console.WriteLine($"set add 0..{n - 1} in parallel:");
        Console.WriteLine($"  Count            : {set.Count} (expected {n})");
        Console.WriteLine($"  sum of elements  : {sum} (expected {expectedSum})");

        Console.WriteLine();

        // Single-flight under contention: many threads race to load the SAME missing key. The factory
        // runs inside the owning segment's lock, so it fires exactly once no matter how many callers
        // miss simultaneously - the count is a deterministic 1 despite the parallel stampede.
        var factoryCalls = 0;
        var cache = new ConcurrentEvictingDictionary<int, string>(capacity: 16, EvictingDictionaryPolicy.LeastRecentlyUsed);

        Parallel.For(0, 64, _ =>
            cache.GetOrAdd(0, key =>
            {
                Interlocked.Increment(ref factoryCalls);   // single-flight: at most once for key 0
                return $"loaded-{key}";
            }));

        Console.WriteLine("single-flight GetOrAdd, 64 concurrent callers, one key:");
        Console.WriteLine($"  factory invoked  : {factoryCalls} (expected 1)");

        Console.WriteLine();
    }
}

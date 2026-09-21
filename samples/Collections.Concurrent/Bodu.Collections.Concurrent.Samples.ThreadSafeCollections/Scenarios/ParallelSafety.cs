// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelSafety.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;
using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates that the concurrent collections stay correct under genuine parallelism: a
/// <see cref="System.Threading.Tasks.Parallel" /> workload mutates them from many threads at once, and the scenario
/// reports only the aggregates that a correct implementation must produce regardless of interleaving.
/// </summary>
/// <remarks>
/// Every other scenario in this sample runs single-threaded so its transcript is exact. This one does the opposite
/// and keeps the output deterministic a different way: it prints no per-item result, because those genuinely do
/// arrive in an unpredictable order. What it prints instead are invariants — a count, a sum, and a call tally —
/// each of which has exactly one correct value no matter how the threads interleave. A sample that printed items
/// here would either be non-deterministic or be quietly serializing the work it claims to parallelize.
/// </remarks>
public static class ParallelSafety
{
    /// <summary>
    /// Runs a parallel add-all into a set and a parallel single-flight load, printing the invariants that hold for
    /// any interleaving.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Parallel safety - invariants that hold for any interleaving",
            what: "Adds 0..999 to a ConcurrentHashSet from a parallel loop and checks the count and the sum, then " +
                  "has 64 concurrent callers race to GetOrAdd the same missing key and counts factory invocations.",
            why: "Lock-free is a claim about correctness under contention, and the only honest way to show it is to " +
                  "create the contention and then assert something that cannot accidentally be true. A count proves " +
                  "no add was lost to a torn update; a sum proves no element was corrupted or duplicated into the " +
                  "wrong slot. The single-flight count is the sharpest of the three: the stampede is the exact " +
                  "condition a naive cache fails, and the factory still runs once.",
            expect: "Count 1000 and sum 499500 - the closed form of 0+1+...+999, so a lost or duplicated element " +
                    "would show up even if the count happened to look right. The factory is invoked exactly once " +
                    "across 64 racing callers. These values are fixed; only the timing varies between runs.");

        // Add 0..N-1 into the set from a partitioned Parallel.For. The order in which elements land is
        // nondeterministic, so no individual item is printed - only aggregates that are invariant: the final Count
        // must be N, and the elements must sum to 0 + 1 + ... + (N-1).
        const int n = 1000;
        var set = new ConcurrentHashSet<int>();

        Parallel.For(0, n, i => set.Add(i));

        var sum = 0L;
        foreach (var value in set.ToArray())
            sum += value;

        var expectedSum = (long)n * (n - 1) / 2;

        Console.WriteLine($"  Parallel add of 0..{n - 1} into one set:");
        Console.WriteLine($"    Count          : {set.Count}  (expected {n} - a lost add under contention would show here)");

        // The sum is the stronger check. A count alone can be right while the contents are wrong: one element
        // dropped and another duplicated would still total N. The sum catches that, because it depends on which
        // elements are present rather than how many.
        Console.WriteLine($"    sum            : {sum}  (expected {expectedSum} - catches a swap that the count alone would miss)");
        Console.WriteLine();

        // Single-flight under real contention: many threads race to load the SAME missing key. The factory runs
        // inside the owning segment's lock, so it fires exactly once no matter how many callers miss simultaneously.
        // This is the thundering-herd case from the SingleFlightCache scenario, now with an actual herd.
        var factoryCalls = 0;
        var cache = new ConcurrentEvictingDictionary<int, string>(capacity: 16, EvictingDictionaryPolicy.LeastRecentlyUsed);

        Parallel.For(0, 64, _ =>
            cache.GetOrAdd(0, key =>
            {
                Interlocked.Increment(ref factoryCalls);   // single-flight: at most once for key 0
                return $"loaded-{key}";
            }));

        Console.WriteLine("  64 concurrent callers racing to load one missing key:");
        Console.WriteLine($"    factory calls  : {factoryCalls}  (expected 1 - the other 63 waited and took the loaded value)");

        Console.WriteLine();
    }
}

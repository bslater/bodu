// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections;

/// <summary>
/// Entry point for the thread-safe collections sample: the <c>Bodu.Collections.Generic.Concurrent</c>
/// primitives — <c>ConcurrentCircularBuffer&lt;T&gt;</c> (a lock-free bounded ring),
/// <c>ConcurrentHashSet&lt;T&gt;</c> (a lock-free split-ordered set), and
/// <c>ConcurrentEvictingDictionary&lt;TKey, TValue&gt;</c> (a lock-striped bounded cache with
/// single-flight <c>GetOrAdd</c> and an <c>ItemEvicted</c> callback). Everything runs offline and
/// prints deterministic output — the parallel scenario reports only order-independent aggregates.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Concurrent.Samples.ThreadSafeCollections");
        Console.WriteLine("========================================================");
        Console.WriteLine();

        BoundedRingBuffer.Run();
        LockFreeSet.Run();
        SingleFlightCache.Run();
        ParallelSafety.Run();

        Console.WriteLine("Done.");
    }
}

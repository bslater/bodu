// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoundedRingBuffer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentCircularBuffer{T}" />: a lock-free, fixed-capacity FIFO ring. It
/// shows the two overflow modes — reject-when-full (the <see cref="IProducerConsumerCollection{T}" />
/// <c>TryAdd</c>/<c>TryTake</c> contract) and overwrite-oldest with an <c>ItemEvicted</c> callback —
/// all on a single thread so the FIFO order is observable and deterministic.
/// </summary>
public static class BoundedRingBuffer
{
    /// <summary>
    /// Fills a reject-on-full ring through the producer/consumer interface, then an overwrite ring that
    /// evicts its oldest element.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ConcurrentCircularBuffer<T>: bounded FIFO ring ---");

        // The element type is constrained to a reference type (where T : class?), so we use string.
        // Part 1: a reject-on-full ring, driven through the IProducerConsumerCollection<T> surface so
        // TryAdd/TryTake read exactly as they would when wrapped by a BlockingCollection<T>.
        var reject = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: false);
        IProducerConsumerCollection<string> pc = reject;

        Console.WriteLine($"TryAdd A         : {pc.TryAdd("A")}");
        Console.WriteLine($"TryAdd B         : {pc.TryAdd("B")}");
        Console.WriteLine($"TryAdd C         : {pc.TryAdd("C")}");
        Console.WriteLine($"TryAdd D (full)  : {pc.TryAdd("D")}");   // rejected: capacity reached, no overwrite
        Console.WriteLine($"Count / Capacity : {reject.Count} / {reject.Capacity}");

        // TryTake drains in first-in, first-out order.
        pc.TryTake(out var first);
        pc.TryTake(out var second);
        Console.WriteLine($"TryTake x2 (FIFO): {first}, {second}");

        Console.WriteLine();

        // Part 2: an overwrite ring. Enqueuing into a full buffer evicts the oldest element and raises
        // ItemEvicted after removal. We collect the evicted elements to show which were displaced.
        var evicted = new List<string>();
        var ring = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: true);
        ring.ItemEvicted += item => evicted.Add(item);

        foreach (var item in new[] { "A", "B", "C", "D", "E" })
            ring.Enqueue(item);   // A and B are pushed out as D and E arrive

        Console.WriteLine($"overwrite evicted: [{string.Join(", ", evicted)}]");
        Console.WriteLine($"survivors (FIFO) : [{string.Join(", ", ring.ToArray())}]");

        Console.WriteLine();
    }
}

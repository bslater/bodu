// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoundedRingBuffer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentCircularBuffer{T}" />: a lock-free, fixed-capacity FIFO ring, and the single
/// decision that separates its two personalities — what a write does when the ring is already full.
/// </summary>
/// <remarks>
/// Both personalities run on one thread here. The overflow rule is a property of the buffer, not of the threading,
/// so driving it sequentially makes the FIFO order observable and the transcript reproducible; the lock-free
/// protocol underneath is what makes the same calls safe from many threads at once.
/// </remarks>
public static class BoundedRingBuffer
{
    /// <summary>
    /// Fills a reject-on-full ring through the producer/consumer interface, then an overwrite ring that evicts its
    /// oldest element, printing which elements were displaced and which survived.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ConcurrentCircularBuffer<T> - bounded FIFO ring",
            what: "Fills a capacity-3 ring past its limit twice: once with allowOverwrite false, driven through the " +
                  "IProducerConsumerCollection<T> surface, and once with allowOverwrite true, collecting the " +
                  "ItemEvicted callbacks.",
            why: "A bounded buffer has to do something when it fills, and the two sensible answers suit opposite " +
                 "jobs. Reject-on-full applies back-pressure - the producer learns it is outrunning the consumer and " +
                 "can slow down or shed load. Overwrite-oldest never blocks a producer and silently discards history, " +
                 "which is what you want for a telemetry ring or a crash buffer holding the last N events. Choosing " +
                 "the wrong one is how a queue either deadlocks or quietly loses data.",
            expect: "The first ring accepts A, B, C and refuses D, so TryAdd returns False and Count stays at the " +
                    "capacity of 3. The second ring accepts all five, evicting A and B - the two oldest - and keeps " +
                    "C, D, E in arrival order.");

        // The element type is constrained to a reference type (where T : class?), so string is used throughout.
        // Part 1: a reject-on-full ring, driven through the IProducerConsumerCollection<T> surface so TryAdd and
        // TryTake read exactly as they would when this buffer is wrapped by a BlockingCollection<T>.
        var reject = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: false);
        IProducerConsumerCollection<string> pc = reject;

        Console.WriteLine("  Reject-on-full (allowOverwrite: false) - the back-pressure personality:");
        Console.WriteLine($"    TryAdd A       : {pc.TryAdd("A")}  (expected True - one of three slots)");
        Console.WriteLine($"    TryAdd B       : {pc.TryAdd("B")}  (expected True)");
        Console.WriteLine($"    TryAdd C       : {pc.TryAdd("C")}  (expected True - the ring is now exactly full)");

        // The refusal is the whole point: TryAdd reports the failure instead of throwing or discarding, so a
        // producer can react. Nothing already in the ring is disturbed.
        Console.WriteLine($"    TryAdd D (full): {pc.TryAdd("D")}  (expected False - full, and no element is displaced)");
        Console.WriteLine($"    Count/Capacity : {reject.Count} / {reject.Capacity}  (expected 3 / 3 - the refusal left the contents untouched)");

        // TryTake drains in first-in, first-out order: A was written first, so it comes back first.
        pc.TryTake(out var first);
        pc.TryTake(out var second);
        Console.WriteLine($"    TryTake x2     : {first}, {second}  (expected A, B - FIFO, oldest first)");
        Console.WriteLine();

        // Part 2: the overwrite ring. Enqueuing into a full buffer evicts the oldest element and raises ItemEvicted
        // *after* the removal has committed, so a handler always observes a consistent buffer.
        var evicted = new List<string>();
        var ring = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: true);
        ring.ItemEvicted += item => evicted.Add(item);

        Console.WriteLine("  Overwrite-oldest (allowOverwrite: true) - the never-block personality:");

        foreach (var item in new[] { "A", "B", "C", "D", "E" })
            ring.Enqueue(item);   // A and B are pushed out as D and E arrive; Enqueue never fails

        // The callback is how a consumer learns what it lost. Without it an overwrite ring discards silently, which
        // is exactly the failure mode that makes dropped telemetry so hard to notice.
        Console.WriteLine($"    evicted        : [{string.Join(", ", evicted)}]  (expected [A, B] - the two oldest, in the order they were displaced)");

        // ToArray materializes a point-in-time snapshot in FIFO order. Under real concurrency this is the safe way
        // to observe contents: enumerating live state races with writers, a snapshot cannot.
        Console.WriteLine($"    survivors      : [{string.Join(", ", ring.ToArray())}]  (expected [C, D, E] - the newest three, oldest first)");
        Console.WriteLine($"    Count/Capacity : {ring.Count} / {ring.Capacity}  (expected 3 / 3 - a full ring stays full; overwriting is not growth)");

        Console.WriteLine();
    }
}

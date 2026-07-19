// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingAndDeque.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates the two fixed-capacity sequential buffers: <see cref="CircularBuffer{T}" /> as an
/// overwrite-on-full FIFO ring, and <see cref="Deque{T}" /> as a double-ended queue whose overflow behaviour
/// is governed by a <see cref="DequeOverflowPolicy" />.
/// </summary>
public static class RingAndDeque
{
    /// <summary>
    /// Fills a ring past its capacity to show overwrite eviction, then pushes and pops a deque at both ends.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CircularBuffer<T> and Deque<T>: fixed-capacity buffers ---");

        RunCircularBuffer();
        RunDeque();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows a capacity-3 ring that overwrites its oldest element once full, reporting each eviction.
    /// </summary>
    private static void RunCircularBuffer()
    {
        // allowOverwrite: true makes Enqueue on a full ring drop the oldest element instead of throwing.
        var ring = new CircularBuffer<int>(capacity: 3, allowOverwrite: true);

        // The ItemEvicted event fires with the value the ring dropped to make room - deterministic order.
        ring.ItemEvicted += evicted => Console.WriteLine($"  ring evicted : {evicted}");

        // Enqueue five values into three slots: 1 and 2 are overwritten by 4 and 5.
        foreach (var value in new[] { 1, 2, 3, 4, 5 })
            ring.Enqueue(value);

        // Enumeration walks head -> tail, so the survivors print in FIFO order (oldest first).
        Console.WriteLine($"  ring survivors (cap {ring.Capacity}) : {string.Join(", ", ring)}");
        Console.WriteLine($"  ring peek (oldest)   : {ring.Peek()}");
        Console.WriteLine($"  ring dequeue (oldest): {ring.Dequeue()}");
    }

    /// <summary>
    /// Shows a bounded deque adding at both ends and, once full, evicting the opposite end per its policy.
    /// </summary>
    private static void RunDeque()
    {
        // A bounded deque (allowGrow: false) with EvictOpposite: an AddLast on a full deque drops the head.
        var deque = new Deque<string>(capacity: 3, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        deque.ItemEvicted += evicted => Console.WriteLine($"  deque evicted: {evicted}");

        // Build "b, c" then push "a" onto the head so the order is a, b, c.
        deque.AddLast("b");
        deque.AddLast("c");
        deque.AddFirst("a");

        // The deque is full (3/3); AddLast("d") evicts the opposite end - the head "a".
        deque.AddLast("d");

        // Enumeration walks first -> last.
        Console.WriteLine($"  deque contents (cap {deque.Capacity}): {string.Join(", ", deque)}");
        Console.WriteLine($"  deque first / last   : {deque.PeekFirst()} / {deque.PeekLast()}");
    }
}

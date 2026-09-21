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
/// <remarks>
/// Both are fixed-capacity, and the interesting question for either is the same one: what happens on a write that
/// does not fit. A ring has one sensible answer — drop the oldest — but a double-ended queue has two ends, so the
/// answer has to be configurable, which is what <see cref="DequeOverflowPolicy" /> exists for.
/// </remarks>
public static class RingAndDeque
{
    /// <summary>
    /// Fills a ring past its capacity to show overwrite eviction, then pushes and pops a deque at both ends.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "CircularBuffer<T> and Deque<T> - fixed-capacity buffers",
            what: "Pushes five values into a capacity-3 ring that overwrites, reporting each eviction, then builds " +
                  "a bounded deque from both ends and overflows it under the EvictOpposite policy.",
            why: "A fixed-capacity buffer never grows, so its whole character is decided by what a write does when " +
                 "it does not fit. A ring has one end to drop from, so overwrite-oldest is the only sensible " +
                 "answer and the ItemEvicted callback is how you learn what went. A deque has two, so the choice " +
                 "is real: EvictOpposite makes an AddLast drop the head, which is what turns a deque into a " +
                 "sliding window over a stream rather than a queue that refuses work.",
            expect: "The ring evicts 1 then 2 and keeps 3, 4, 5 in FIFO order, so Peek and Dequeue both return 3 - " +
                    "the oldest, not the newest. The deque is built as a, b, c and then AddLast(\"d\") evicts a " +
                    "from the far end, leaving b, c, d.");

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
        ring.ItemEvicted += evicted => Console.WriteLine($"  ring evicted : {evicted}  (dropped to make room; without this callback the loss would be silent)");

        // Enqueue five values into three slots: 1 and 2 are overwritten by 4 and 5.
        foreach (var value in new[] { 1, 2, 3, 4, 5 })
            ring.Enqueue(value);

        // Enumeration walks head -> tail, so the survivors print in FIFO order (oldest first).
        Console.WriteLine($"  ring survivors (cap {ring.Capacity}) : {string.Join(", ", ring)}  (expected 3, 4, 5 - enumeration walks head to tail, so oldest prints first)");
        Console.WriteLine($"  ring peek (oldest)   : {ring.Peek()}  (expected 3 - a FIFO ring reads from the oldest end, not the newest)");
        Console.WriteLine($"  ring dequeue (oldest): {ring.Dequeue()}  (expected 3 - same element Peek just reported, now removed)");
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

        deque.ItemEvicted += evicted => Console.WriteLine($"  deque evicted: {evicted}  (expected a - the head, because the write came in at the tail)");

        // Build "b, c" then push "a" onto the head so the order is a, b, c.
        deque.AddLast("b");
        deque.AddLast("c");
        deque.AddFirst("a");

        // The deque is full (3/3); AddLast("d") evicts the opposite end - the head "a".
        deque.AddLast("d");

        // Enumeration walks first -> last.
        Console.WriteLine($"  deque contents (cap {deque.Capacity}): {string.Join(", ", deque)}  (expected b, c, d - the window slid by one rather than the write being refused)");
        Console.WriteLine($"  deque first / last   : {deque.PeekFirst()} / {deque.PeekLast()}  (expected b / d - both ends are O(1) to read, which is the point of a deque)");
    }
}

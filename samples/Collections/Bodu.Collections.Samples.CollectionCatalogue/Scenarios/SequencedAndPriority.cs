// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedAndPriority.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates <see cref="SequencedDictionary{TKey, TValue}" /> (a dictionary that enumerates keys in
/// insertion order with cheap first/last access) and
/// <see cref="IndexedPriorityQueue{TElement, TPriority}" /> (a min-priority queue that additionally supports
/// updating an already-queued element's priority — the "decrease-key" operation Dijkstra needs).
/// </summary>
/// <remarks>
/// Decrease-key is the reason the queue is <em>indexed</em>. A plain priority queue has no way to find an element
/// it has already accepted, so the usual workaround is to push a duplicate at the new priority and discard stale
/// entries on the way out — which inflates the heap and forces every consumer to carry that filtering logic.
/// </remarks>
public static class SequencedAndPriority
{
    /// <summary>
    /// Enumerates a sequenced dictionary in insertion order, then drains a priority queue after a decrease-key.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "SequencedDictionary / IndexedPriorityQueue",
            what: "Enumerates a pipeline definition in insertion order and removes from the front, then lowers an " +
                  "already-queued item's priority and drains the queue to show where it landed.",
            why: "Dictionary enumeration order is explicitly unspecified, so anything order-sensitive - a pipeline, " +
                 "a migration list, an ordered config - cannot use one without a parallel list to remember the " +
                 "sequence. SequencedDictionary makes that order part of the type. IndexedPriorityQueue answers a " +
                 "different gap: a standard heap cannot reach an element once queued, so lowering a cost means " +
                 "pushing a duplicate and filtering stale pops later. Dijkstra and A* need exactly that operation, " +
                 "which is why the indexed form exists.",
            expect: "The dictionary enumerates in insertion order rather than hash order, and First and Last are " +
                    "O(1) rather than a scan. In the queue, parse starts at priority 40 - behind three others - " +
                    "and after the decrease-key to 5 it drains second, having moved without being re-added.");

        RunSequencedDictionary();
        RunIndexedPriorityQueue();

        Console.WriteLine();
    }

    /// <summary>
    /// Inserts keys out of alphabetical order to show that enumeration follows insertion, not key, order.
    /// </summary>
    private static void RunSequencedDictionary()
    {
        // Unlike SortedDictionary, a sequenced dictionary preserves the order keys were first added.
        var steps = new SequencedDictionary<string, int>
        {
            ["clone"] = 1,
            ["build"] = 2,
            ["test"] = 3,
            ["ship"] = 4,
        };

        // Enumeration honours insertion order, so the pipeline reads clone -> build -> test -> ship.
        Console.WriteLine($"  pipeline order : {string.Join(" -> ", steps.Keys)}  (insertion order, guaranteed - a Dictionary would be free to print these in any order at all)");
        Console.WriteLine($"  first / last   : {steps.First.Key} / {steps.Last.Key}  (both O(1); finding the ends of an ordinary dictionary means enumerating it)");

        // TryRemoveFirst pops the oldest entry - the head of the sequence.
        steps.TryRemoveFirst(out var removed);
        Console.WriteLine($"  removed first  : {removed.Key}  (removing from the front is O(1) too, so this doubles as an ordered work queue)");
        Console.WriteLine($"  remaining      : {string.Join(" -> ", steps.Keys)}  (the surviving entries keep their relative order - removal does not reshuffle)");
    }

    /// <summary>
    /// Queues tasks by cost, lowers one task's priority in place, then dequeues in ascending-priority order.
    /// </summary>
    private static void RunIndexedPriorityQueue()
    {
        // Default comparer -> min-heap: the smallest priority is dequeued first.
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Enqueue("index", 30);
        queue.Enqueue("render", 20);
        queue.Enqueue("parse", 40);
        queue.Enqueue("lex", 10);

        Console.WriteLine($"  peek (min)     : {queue.Peek().Key} @ {queue.Peek().Value}  (a min-queue, so the lowest priority is the front)");

        // Decrease-key: "parse" was 40; lower it to 5 so it now sorts ahead of everything else.
        queue.Update("parse", 5);
        Console.WriteLine("  decreased \u0027parse\u0027 priority 40 -> 5  - the element moves within the heap; nothing is re-added and nothing stale is left behind");

        // Draining the queue yields elements in ascending priority: parse(5), lex(10), render(20), index(30).
        var drained = new List<string>();
        while (queue.TryDequeue(out var element, out var priority))
            drained.Add($"{element}({priority})");

        Console.WriteLine($"  drain order    : {string.Join(", ", drained)}  (parse comes out second, at its new priority - with a plain heap it would still be sitting at 40)");
    }
}

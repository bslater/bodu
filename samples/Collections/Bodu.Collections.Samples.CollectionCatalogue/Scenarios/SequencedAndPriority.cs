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
public static class SequencedAndPriority
{
    /// <summary>
    /// Enumerates a sequenced dictionary in insertion order, then drains a priority queue after a decrease-key.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- SequencedDictionary / IndexedPriorityQueue ---");

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
        Console.WriteLine($"  pipeline order : {string.Join(" -> ", steps.Keys)}");
        Console.WriteLine($"  first / last   : {steps.First.Key} / {steps.Last.Key}");

        // TryRemoveFirst pops the oldest entry - the head of the sequence.
        steps.TryRemoveFirst(out var removed);
        Console.WriteLine($"  removed first  : {removed.Key}");
        Console.WriteLine($"  remaining      : {string.Join(" -> ", steps.Keys)}");
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

        Console.WriteLine($"  peek (min)     : {queue.Peek().Key} @ {queue.Peek().Value}");

        // Decrease-key: "parse" was 40; lower it to 5 so it now sorts ahead of everything else.
        queue.Update("parse", 5);
        Console.WriteLine("  decreased 'parse' priority 40 -> 5");

        // Draining the queue yields elements in ascending priority: parse(5), lex(10), render(20), index(30).
        var drained = new List<string>();
        while (queue.TryDequeue(out var element, out var priority))
            drained.Add($"{element}({priority})");

        Console.WriteLine($"  drain order    : {string.Join(", ", drained)}");
    }
}

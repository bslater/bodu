// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableOperators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Extensions;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates the LINQ-style operators added by <c>IEnumerableExtensions</c> in
/// <c>Bodu.Collections.Generic.Extensions</c>: sequence-shaping combinators the BCL does not ship, such as
/// fixed-size batching, sliding windows, adjacent pairing, running accumulation, run-length encoding, and
/// length-tolerant zipping. All are deferred and deterministic over the same fixed input.
/// </summary>
public static class EnumerableOperators
{
    /// <summary>
    /// Applies each operator to a fixed integer (or string) source and prints the shaped result.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "IEnumerableExtensions - sequence-shaping operators",
            what: "Runs one seven-element sequence through Batch, Windowed, Pairwise, Scan, RunLengthEncode, " +
                  "Interleave and ZipLongest.",
            why: "These are the operators LINQ leaves out, and the hand-written versions are where off-by-one " +
                 "errors live. Batch and Windowed are the pair most often confused: batching partitions, so every " +
                 "element appears once and the final batch may be short; windowing slides, so elements repeat and " +
                 "the count is n - size + 1. ZipLongest matters for the opposite reason - Zip stops at the shorter " +
                 "input and silently drops the tail, which is a data-loss bug rather than a formatting one.",
            expect: "Batch(3) yields three groups with a short final one; Windowed(3) yields five overlapping " +
                    "groups from the same seven elements. Scan shows running totals rather than just the final " +
                    "sum, and ZipLongest pads the shorter side with a default instead of truncating.");

        var numbers = new[] { 1, 2, 3, 4, 5, 6, 7 };

        // Batch splits the source into consecutive fixed-size groups (the final group may be short).
        // The second argument is a per-element selector applied while batching; the identity lambda keeps values as-is.
        var batches = numbers.Batch(3, x => x).Select(b => $"[{string.Join(' ', b)}]");
        Console.WriteLine($"  Batch(3)         : {string.Join(' ', batches)}  (partitions: every element appears exactly once and the last batch is short)");

        // Windowed yields every complete overlapping window of the given size, advancing one element at a time.
        var windows = numbers.Windowed(3).Select(w => $"[{string.Join(' ', w)}]");
        Console.WriteLine($"  Windowed(3)      : {string.Join(' ', windows)}  (slides: elements repeat across windows, and seven elements give n - size + 1 = 5 of them)");

        // Pairwise emits each adjacent (previous, current) pair - one fewer than the element count.
        var pairs = numbers.Pairwise().Select(p => $"({p.Previous},{p.Current})");
        Console.WriteLine($"  Pairwise         : {string.Join(' ', pairs)}  (Windowed(2) in tuple form - the idiomatic way to compare each element with its predecessor)");

        // Scan is a running fold: it emits every intermediate accumulator, here a prefix-sum.
        var runningSum = numbers.Scan(0, (acc, x) => acc + x);
        Console.WriteLine($"  Scan (prefix +)  : {string.Join(' ', runningSum)}  (running totals, so the final value equals Aggregate while every intermediate step stays visible)");

        // RunLengthEncode collapses consecutive equal values into (value, count) pairs.
        var runs = new[] { 'a', 'a', 'a', 'b', 'b', 'c' };
        var rle = runs.RunLengthEncode().Select(r => $"{r.Value}x{r.Count}");
        Console.WriteLine($"  RunLengthEncode  : {string.Join(' ', rle)}  (collapses only CONSECUTIVE equal elements, which is what makes it a streaming operation)");

        // Interleave round-robins one element from each source until all are exhausted.
        var interleaved = new[] { 1, 2, 3 }.Interleave(new[] { 10, 20, 30 });
        Console.WriteLine($"  Interleave       : {string.Join(' ', interleaved)}  (takes alternately from each source rather than concatenating them)");

        // ZipLongest pairs elements up to the LONGER sequence, padding the shorter side with its default.
        var zipped = new[] { 1, 2, 3 }.ZipLongest(new[] { 100, 200 }).Select(z => $"({z.First},{z.Second})");
        Console.WriteLine($"  ZipLongest       : {string.Join(' ', zipped)}  (the third pair is (3,0): the shorter side is padded with a default, where Zip would have dropped the row entirely)");

        Console.WriteLine();
    }
}

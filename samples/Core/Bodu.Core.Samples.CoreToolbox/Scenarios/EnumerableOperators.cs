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
        Console.WriteLine("--- IEnumerableExtensions: sequence-shaping operators ---");

        var numbers = new[] { 1, 2, 3, 4, 5, 6, 7 };

        // Batch splits the source into consecutive fixed-size groups (the final group may be short).
        var batches = numbers.Batch(3, x => x).Select(b => $"[{string.Join(' ', b)}]");
        Console.WriteLine($"Batch(3)         : {string.Join(' ', batches)}");

        // Windowed yields every complete overlapping window of the given size, advancing one element at a time.
        var windows = numbers.Windowed(3).Select(w => $"[{string.Join(' ', w)}]");
        Console.WriteLine($"Windowed(3)      : {string.Join(' ', windows)}");

        // Pairwise emits each adjacent (previous, current) pair - one fewer than the element count.
        var pairs = numbers.Pairwise().Select(p => $"({p.Previous},{p.Current})");
        Console.WriteLine($"Pairwise         : {string.Join(' ', pairs)}");

        // Scan is a running fold: it emits every intermediate accumulator, here a prefix-sum.
        var runningSum = numbers.Scan(0, (acc, x) => acc + x);
        Console.WriteLine($"Scan (prefix +)  : {string.Join(' ', runningSum)}");

        // RunLengthEncode collapses consecutive equal values into (value, count) pairs.
        var runs = new[] { 'a', 'a', 'a', 'b', 'b', 'c' };
        var rle = runs.RunLengthEncode().Select(r => $"{r.Value}x{r.Count}");
        Console.WriteLine($"RunLengthEncode  : {string.Join(' ', rle)}");

        // Interleave round-robins one element from each source until all are exhausted.
        var interleaved = new[] { 1, 2, 3 }.Interleave(new[] { 10, 20, 30 });
        Console.WriteLine($"Interleave       : {string.Join(' ', interleaved)}");

        // ZipLongest pairs elements up to the LONGER sequence, padding the shorter side with its default.
        var zipped = new[] { 1, 2, 3 }.ZipLongest(new[] { 100, 200 }).Select(z => $"({z.First},{z.Second})");
        Console.WriteLine($"ZipLongest       : {string.Join(' ', zipped)}");

        Console.WriteLine();
    }
}

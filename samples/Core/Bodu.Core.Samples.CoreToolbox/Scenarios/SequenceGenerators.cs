// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Sequences;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates the <see cref="SequenceGenerator" /> catalogue: a family of lazily evaluated, deterministic
/// integer / rational / string sequences. Each generator here is <em>bounded</em> — by a value window
/// (<c>Fibonacci</c>) or an element count (<c>ThueMorse</c>, <c>LookAndSay</c>) — so enumeration terminates
/// and the printed output is finite.
/// </summary>
public static class SequenceGenerators
{
    /// <summary>
    /// Materializes a handful of bounded sequences and prints each one on a single line.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "SequenceGenerator - bounded, deterministic sequences",
            what: "Produces arithmetic ranges in both directions, then four classic integer sequences - Fibonacci " +
                  "windowed by value, Thue-Morse, look-and-say, and the Farey fractions of order 5.",
            why: "Every one of these is lazy and bounded at the source rather than by the caller. That matters for " +
                 "the unbounded ones: Fibonacci is filtered by value range, not by taking a count and hoping, so " +
                 "nothing computes a term past the bound. Generating a sequence and then trimming it is the " +
                 "version that either overshoots or runs forever, and it is what these exist to avoid.",
            expect: "Range counts down as readily as up when given a negative step. The Fibonacci window starts at " +
                    "13 - the first term at or above 10 - and stops below 1000. Farey(5) lists every fraction in " +
                    "lowest terms between 0/1 and 1/1 with denominator at most 5, in ascending order.");

        // Range(start, stop) yields an inclusive ascending run with an inferred +1 step.
        var range = SequenceGenerator.Range(1, 10);
        Console.WriteLine($"  Range(1, 10)      : {string.Join(' ', range)}  (ascending, inclusive of both ends)");

        // Range(start, stop, step) accepts an explicit stride, including descending ranges.
        var countdown = SequenceGenerator.Range(20, 0, -5);
        Console.WriteLine($"  Range(20, 0, -5)  : {string.Join(' ', countdown)}  (a negative step counts down - no separate Reverse and no manual loop)");

        // Fibonacci(min, max) emits the Fibonacci numbers that fall in the half-open window [min, max).
        var fib = SequenceGenerator.Fibonacci(10, 1000);
        Console.WriteLine($"  Fibonacci[10,1000): {string.Join(' ', fib)}  (starts at 13, the first term >= 10, and stops below 1000; bounded by VALUE, so no term past the limit is ever computed)");

        // ThueMorse(count) produces the first N bits of the (non-periodic, cube-free) Thue-Morse sequence.
        var thue = SequenceGenerator.ThueMorse(16);
        Console.WriteLine($"  ThueMorse(16)     : {string.Join(' ', thue)}  (each bit is the parity of the set bits in its index - famously cube-free, and generated lazily)");

        // LookAndSay(count) yields the first N "look-and-say" terms, each read off the previous one.
        var lookAndSay = SequenceGenerator.LookAndSay(6);
        Console.WriteLine($"  LookAndSay(6)     : {string.Join(", ", lookAndSay)}  (each term describes the previous one aloud: 1211 reads as one 1, one 2, two 1s)");

        // Farey(order) enumerates the Farey sequence of the given order as (numerator, denominator) pairs,
        // in strictly increasing rational order from 0/1 to 1/1.
        var farey = SequenceGenerator.Farey(5).Select(f => $"{f.Numerator}/{f.Denominator}");
        Console.WriteLine($"  Farey(5)          : {string.Join(' ', farey)}  (every fraction in lowest terms with denominator <= 5, already in ascending order)");

        Console.WriteLine();
    }
}

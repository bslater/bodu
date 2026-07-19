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
        Console.WriteLine("--- SequenceGenerator: bounded, deterministic sequences ---");

        // Range(start, stop) yields an inclusive ascending run with an inferred +1 step.
        var range = SequenceGenerator.Range(1, 10);
        Console.WriteLine($"Range(1, 10)      : {string.Join(' ', range)}");

        // Range(start, stop, step) accepts an explicit stride, including descending ranges.
        var countdown = SequenceGenerator.Range(20, 0, -5);
        Console.WriteLine($"Range(20, 0, -5)  : {string.Join(' ', countdown)}");

        // Fibonacci(min, max) emits the Fibonacci numbers that fall in the half-open window [min, max).
        var fib = SequenceGenerator.Fibonacci(10, 1000);
        Console.WriteLine($"Fibonacci[10,1000): {string.Join(' ', fib)}");

        // ThueMorse(count) produces the first N bits of the (non-periodic, cube-free) Thue-Morse sequence.
        var thue = SequenceGenerator.ThueMorse(16);
        Console.WriteLine($"ThueMorse(16)     : {string.Join(' ', thue)}");

        // LookAndSay(count) yields the first N "look-and-say" terms, each read off the previous one.
        var lookAndSay = SequenceGenerator.LookAndSay(6);
        Console.WriteLine($"LookAndSay(6)     : {string.Join(", ", lookAndSay)}");

        // Farey(order) enumerates the Farey sequence of the given order as (numerator, denominator) pairs,
        // in strictly increasing rational order from 0/1 to 1/1.
        var farey = SequenceGenerator.Farey(5).Select(f => $"{f.Numerator}/{f.Denominator}");
        Console.WriteLine($"Farey(5)          : {string.Join(' ', farey)}");

        Console.WriteLine();
    }
}

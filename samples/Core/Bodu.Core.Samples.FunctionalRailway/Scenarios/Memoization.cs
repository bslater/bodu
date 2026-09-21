// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Memoization.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Functional;

namespace Bodu.Core.Samples.FunctionalRailway.Scenarios;

/// <summary>
/// Demonstrates <see cref="Memoizer" />: wrapping a pure function so repeated calls with the same
/// argument return the cached result instead of recomputing. The invocation counter is the
/// load-bearing evidence — it advances once per distinct argument, never per call.
/// </summary>
public static class Memoization
{
    /// <summary>
    /// Wraps a counted function and shows the underlying function runs once per distinct argument.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Memoizer - cache a pure function",
            what: "Wraps a counting function, calls it ten times across four distinct arguments, and reports how " +
                  "often the underlying function actually ran.",
            why: "Memoization is only sound for a pure function - same input, same output, no side effects - " +
                 "because the cache will happily serve a stale answer forever otherwise. Given that, it turns " +
                 "repeated work into a lookup with no change to the call sites. The invocation counter is the " +
                 "load-bearing evidence here: without it, a memoized and a non-memoized function are " +
                 "indistinguishable from their return values alone, which is exactly why this scenario counts " +
                 "rather than just printing results.",
            expect: "Ten calls, four distinct arguments, and the function body runs four times - once per distinct " +
                    "argument. The final call re-requests an argument already seen, and the counter does not move.");

        var calls = 0;

        // A deterministic but "expensive" pure function. Memoize returns a caching delegate.
        Func<int, long> square = Memoizer.Memoize<int, long>(n =>
        {
            calls++;
            return (long)n * n;
        });

        // Ten calls over four distinct arguments.
        foreach (var n in new[] { 3, 5, 3, 8, 5, 3, 8, 5, 3, 13 })
            _ = square(n);

        Console.WriteLine($"  calls made       : 10  (through the memoized wrapper, which is what every call site sees)");
        Console.WriteLine($"  distinct args    : 4 (3, 5, 8, 13)  (the cache is keyed on the argument, so this is the upper bound on real work)");
        Console.WriteLine($"  function invoked : {calls} time(s)  (expected 4 - six of the ten calls were served from the cache without entering the function)");
        Console.WriteLine($"  square(13)       : {square(13)}  (counter still {calls} - an already-seen argument costs a lookup, and the function is not re-entered)");

        Console.WriteLine();
    }
}

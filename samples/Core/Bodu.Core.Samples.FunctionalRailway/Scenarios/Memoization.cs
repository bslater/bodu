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
        Console.WriteLine("--- Memoizer: cache a pure function ---");

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

        Console.WriteLine($"calls made       : 10");
        Console.WriteLine($"distinct args    : 4 (3, 5, 8, 13)");
        Console.WriteLine($"function invoked : {calls} time(s)");
        Console.WriteLine($"square(13)       : {square(13)} (served from cache, counter unchanged: {calls})");

        Console.WriteLine();
    }
}

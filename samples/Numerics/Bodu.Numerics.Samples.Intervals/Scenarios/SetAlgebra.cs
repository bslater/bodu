// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SetAlgebra.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Intervals.Scenarios;

/// <summary>
/// Demonstrates the two-interval set operations on <see cref="Interval{T}" />: intersection returns
/// a single interval, but subtracting or symmetric-differencing two intervals can leave <em>two</em>
/// disjoint pieces — which is exactly what <see cref="IntervalPair{T}" /> carries, ready to bridge to
/// an <see cref="IntervalSet{T}" /> with <c>ToIntervalSet()</c>.
/// </summary>
public static class SetAlgebra
{
    /// <summary>
    /// Intersects, subtracts, and symmetric-differences two overlapping intervals.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Interval<T>: intersection, difference, union ---");

        var a = Interval<int>.Closed(0, 10);   // [0, 10]
        var b = Interval<int>.Closed(4, 20);   // [4, 20]
        Console.WriteLine($"a = {a}, b = {b}");

        // Intersection is always a single interval (or empty), so it returns Interval<T> directly.
        Console.WriteLine($"a intersect b        : {a.Intersect(b)}");

        // Union of two overlapping intervals is their convex hull - operator | returns one interval.
        Console.WriteLine($"a union b (|)         : {a | b}");

        // Difference (a minus b) can split into two pieces, so it returns an IntervalPair<T>.
        // Here [0,10] minus [4,20] leaves only the left piece [0,4).
        IntervalPair<int> difference = a.Difference(b);
        Console.WriteLine($"a minus b            : {difference} (Count={difference.Count})");

        // Symmetric difference removes the shared middle and keeps both outer pieces - a true pair.
        IntervalPair<int> symmetric = a.SymmetricDifference(b);
        Console.WriteLine($"a symmetric-diff b   : {symmetric} (Count={symmetric.Count})");

        // ToIntervalSet bridges the transient pair into a normalized, enumerable IntervalSet<T>.
        var set = symmetric.ToIntervalSet();
        Console.WriteLine($"...ToIntervalSet()   : {set}");
        Console.WriteLine($"set.Contains(2)      : {set.Contains(2)}, set.Contains(12): {set.Contains(12)}, set.Contains(15): {set.Contains(15)}");

        Console.WriteLine();
    }
}

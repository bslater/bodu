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
        SampleConsole.Scenario(
            "Interval<T> - intersection, difference, union",
            what: "Takes two overlapping spans and computes their intersection, union, difference and symmetric " +
                  "difference, then converts the multi-part results into an IntervalSet.",
            why: "Only intersection is guaranteed to yield a single interval. A union of disjoint spans, and any " +
                 "difference that removes a middle section, produce two pieces - which is why these operations " +
                 "return a pair type rather than one interval, and why hand-written range subtraction so often " +
                 "quietly drops a fragment. The boundary bookkeeping is the other half: removing [4,20] from " +
                 "[0,10] must leave 4 itself outside the result, so the remainder is half-open.",
            expect: "a minus b is [0, 4) - note the open end, because 4 belongs to b. The symmetric difference " +
                    "has two parts and its Count says so, and ToIntervalSet turns that pair into a set that " +
                    "answers Contains across both pieces.");

        var a = Interval<int>.Closed(0, 10);   // [0, 10]
        var b = Interval<int>.Closed(4, 20);   // [4, 20]
        Console.WriteLine($"  a = {a}, b = {b}");

        // Intersection is always a single interval (or empty), so it returns Interval<T> directly.
        Console.WriteLine($"  a intersect b        : {a.Intersect(b)}  (expected [4, 10] - the only one of these operations that always yields a single interval)");

        // Union of two overlapping intervals is their convex hull - operator | returns one interval.
        Console.WriteLine($"  a union b (|)         : {a | b}  (one piece here only because a and b overlap; disjoint inputs would give two)");

        // Difference (a minus b) can split into two pieces, so it returns an IntervalPair<T>.
        // Here [0,10] minus [4,20] leaves only the left piece [0,4).
        IntervalPair<int> difference = a.Difference(b);
        Console.WriteLine($"  a minus b            : {difference} (Count={difference.Count})  (expected [0, 4) - OPEN at 4, because 4 belongs to b; this is the boundary bookkeeping hand-written subtraction gets wrong)");

        // Symmetric difference removes the shared middle and keeps both outer pieces - a true pair.
        IntervalPair<int> symmetric = a.SymmetricDifference(b);
        Console.WriteLine($"  a symmetric-diff b   : {symmetric} (Count={symmetric.Count})  (two pieces, and Count says so - which is why these return a pair type rather than one interval)");

        // ToIntervalSet bridges the transient pair into a normalized, enumerable IntervalSet<T>.
        var set = symmetric.ToIntervalSet();
        Console.WriteLine($"  ...ToIntervalSet()   : {set}");
        Console.WriteLine($"  set.Contains(2)      : {set.Contains(2)}, set.Contains(12): {set.Contains(12)}, set.Contains(15): {set.Contains(15)}  (the set answers across both pieces without the caller checking each one)");

        Console.WriteLine();
    }
}

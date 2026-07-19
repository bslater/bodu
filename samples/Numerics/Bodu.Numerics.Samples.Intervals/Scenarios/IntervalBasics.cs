// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Intervals.Scenarios;

/// <summary>
/// Demonstrates <see cref="Interval{T}" /> over a continuous domain: the closed / open / half-open
/// factories, the empty interval, and the boundary-aware <c>Contains</c> and <c>Overlaps</c>
/// predicates that respect whether each endpoint is inclusive.
/// </summary>
public static class IntervalBasics
{
    /// <summary>
    /// Builds intervals of each inclusivity and probes their membership and overlap behaviour.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Interval<T>: closed, open, half-open ---");

        // The factories differ only in whether each endpoint is part of the set.
        var closed = Interval<double>.Closed(0.0, 10.0);        // [0, 10]
        var open = Interval<double>.Open(0.0, 10.0);            // (0, 10)
        var halfOpen = Interval<double>.ClosedOpen(0.0, 10.0);  // [0, 10)
        Console.WriteLine($"closed    : {closed}");
        Console.WriteLine($"open      : {open}");
        Console.WriteLine($"half-open : {halfOpen}");

        // Contains is boundary-aware: the endpoint 10 belongs to the closed set but not the others.
        Console.WriteLine($"Contains(10): closed={closed.Contains(10.0)}, open={open.Contains(10.0)}, half-open={halfOpen.Contains(10.0)}");

        // The lower endpoint 0 belongs to the closed and closed-open sets, but not the open one.
        Console.WriteLine($"Contains(0) : closed={closed.Contains(0.0)}, open={open.Contains(0.0)}, half-open={halfOpen.Contains(0.0)}");

        // Overlaps reports whether two intervals share any point. Touching at an excluded
        // endpoint does not count as an overlap.
        var upper = Interval<double>.Closed(10.0, 20.0);        // [10, 20]
        Console.WriteLine($"[0,10] overlaps [10,20] : {closed.Overlaps(upper)}");   // share the point 10
        Console.WriteLine($"[0,10) overlaps [10,20] : {halfOpen.Overlaps(upper)}"); // 10 excluded on the left

        // Empty is the canonical no-points interval; nothing is a member.
        var empty = Interval<double>.Empty;
        Console.WriteLine($"empty     : {empty} (IsEmpty={empty.IsEmpty}, Contains(0)={empty.Contains(0.0)})");

        Console.WriteLine();
    }
}

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
        SampleConsole.Scenario(
            "Interval<T> - closed, open, half-open",
            what: "Builds the same 0-to-10 span three ways, asks each whether it contains its own endpoints, then " +
                  "tests two adjacent spans for overlap under two different boundary conventions.",
            why: "Whether an endpoint is included is not a detail - it decides whether adjacent ranges touch or " +
                  "overlap, and getting it wrong double-counts the boundary. That is the bug behind a reading " +
                  "filed in two buckets, an event billed to two periods, or a schedule that claims a conflict it " +
                  "does not have. Making the convention part of the type means it travels with the value instead " +
                  "of living in a comment beside a pair of comparisons.",
            expect: "The three spans differ only at the endpoints, and that is enough to change the answers: " +
                    "[0,10] and [10,20] overlap at 10, while [0,10) and [10,20] tile without touching. The empty " +
                    "interval contains nothing at all, including the values its bounds name.");

        // The factories differ only in whether each endpoint is part of the set.
        var closed = Interval<double>.Closed(0.0, 10.0);        // [0, 10]
        var open = Interval<double>.Open(0.0, 10.0);            // (0, 10)
        var halfOpen = Interval<double>.ClosedOpen(0.0, 10.0);  // [0, 10)
        Console.WriteLine($"  closed    : {closed}");
        Console.WriteLine($"  open      : {open}");
        Console.WriteLine($"  half-open : {halfOpen}");

        // Contains is boundary-aware: the endpoint 10 belongs to the closed set but not the others.
        Console.WriteLine($"  Contains(10): closed={closed.Contains(10.0)}, open={open.Contains(10.0)}, half-open={halfOpen.Contains(10.0)}  (the upper endpoint: in for closed, out for the other two - one character of syntax, three different answers)");

        // The lower endpoint 0 belongs to the closed and closed-open sets, but not the open one.
        Console.WriteLine($"  Contains(0) : closed={closed.Contains(0.0)}, open={open.Contains(0.0)}, half-open={halfOpen.Contains(0.0)}  (and the lower endpoint, where half-open sides with closed rather than open)");

        // Overlaps reports whether two intervals share any point. Touching at an excluded
        // endpoint does not count as an overlap.
        var upper = Interval<double>.Closed(10.0, 20.0);        // [10, 20]
        Console.WriteLine($"  [0,10] overlaps [10,20] : {closed.Overlaps(upper)}  (expected True - both claim 10, so these two ranges double-count their shared boundary)");   // share the point 10
        Console.WriteLine($"  [0,10) overlaps [10,20] : {halfOpen.Overlaps(upper)}  (expected False - the half-open form tiles cleanly, which is why it is the right default for buckets and billing periods)"); // 10 excluded on the left

        // Empty is the canonical no-points interval; nothing is a member.
        var empty = Interval<double>.Empty;
        Console.WriteLine($"  empty     : {empty} (IsEmpty={empty.IsEmpty}, Contains(0)={empty.Contains(0.0)})  (an empty interval contains nothing at all, including the values its own bounds name)");

        Console.WriteLine();
    }
}

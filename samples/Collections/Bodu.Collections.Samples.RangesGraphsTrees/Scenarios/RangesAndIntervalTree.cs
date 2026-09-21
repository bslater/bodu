// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangesAndIntervalTree.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

/// <summary>
/// Demonstrates the interval-shaped containers: <see cref="RangeSet{T}" /> and
/// <see cref="RangeDictionary{TKey, TValue}" /> coalesce adjacent/overlapping half-open ranges, while
/// <see cref="IntervalTree{TKey, TValue}" /> indexes arbitrary overlapping intervals for point-stabbing and
/// overlap queries.
/// </summary>
/// <remarks>
/// The distinction between the two families is the one to get right. A range set or dictionary <em>coalesces</em>:
/// it assumes ranges do not meaningfully overlap and merges them, so it models a partition such as a grade band or
/// an IP allocation. An interval tree keeps every interval distinct and expects overlap, so it models bookings,
/// reservations and spans that genuinely coexist. Using the first where you needed the second silently merges data.
/// </remarks>
public static class RangesAndIntervalTree
{
    /// <summary>
    /// Coalesces ranges in a set and dictionary, then stabs and overlap-queries an interval tree.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "RangeSet / RangeDictionary / IntervalTree",
            what: "Adds touching and overlapping ranges to a set and watches them coalesce, maps score bands to " +
                  "grades through a range dictionary, then indexes a day of overlapping meetings and asks what is " +
                  "active at an instant and what overlaps a window.",
            why: "Range keys turn a chain of if-else boundary comparisons into a lookup, and boundary comparisons " +
                 "written by hand are where off-by-one bugs live. The critical choice is coalescing versus not: a " +
                 "range set merges adjacent and overlapping ranges, which is right for a partition like a grade " +
                 "band and wrong for anything that legitimately overlaps. An interval tree keeps every interval " +
                 "distinct and answers point-stabbing and window queries in log time rather than by scanning.",
            expect: "[0,10) and [10,20) coalesce into [0,20) because half-open ranges that touch have no gap " +
                    "between them, while [30,40) stays separate. 15 is inside the merged range and 25 falls in the " +
                    "gap. The meeting queries return several overlapping entries at once, which is exactly what " +
                    "the coalescing containers could not represent.");

        RunRangeSet();
        RunRangeDictionary();
        RunIntervalTree();

        Console.WriteLine();
    }

    /// <summary>
    /// Adds three half-open ranges, two of which touch, and shows the set coalescing them.
    /// </summary>
    private static void RunRangeSet()
    {
        // Ranges are half-open [start, end). Adjacent ranges are merged into one on insertion.
        var set = new RangeSet<int>();
        set.Add(0, 10);
        set.Add(10, 20); // touches [0,10) at 10 -> coalesces into [0,20)
        set.Add(30, 40); // disjoint -> stays separate

        // Iterate by index to print the coalesced ranges in ascending order.
        var ranges = Enumerable.Range(0, set.Count).Select(i => $"[{set[i].StartInclusive},{set[i].EndExclusive})");
        Console.WriteLine($"  coalesced ranges : {string.Join(", ", ranges)}  (expected [0,20) and [30,40) - the touching pair merged, since half-open ranges that meet leave no gap; [30,40) is disjoint and survives)");

        // 15 falls inside the coalesced [0,20); 25 sits in the uncovered gap before [30,40).
        Console.WriteLine($"  contains 15 / 25 : {set.Contains(15)} / {set.Contains(25)}  (expected True / False - 15 sits inside the merged range, 25 in the gap between them)");
    }

    /// <summary>
    /// Maps contiguous key ranges to values and looks up the value covering a specific key.
    /// </summary>
    private static void RunRangeDictionary()
    {
        // Each half-open key range carries a value; a point lookup returns the covering range's value.
        var grades = new RangeDictionary<int, string>();
        grades.Add(0, 50, "Fail");
        grades.Add(50, 65, "Pass");
        grades.Add(65, 85, "Credit");
        grades.Add(85, 101, "Distinction");

        Console.WriteLine($"  score 60 -> {grades[60]}  (a band lookup, not a chain of >= comparisons - the boundary lives in the data)");
        Console.WriteLine($"  score 90 -> {grades[90]}  (the top band; note each band is half-open, so 90 belongs to this one and not the one below)");

        // TryGetValue is the non-throwing lookup for keys that may not be covered by any range; the
        // indexer above would throw for an uncovered key.
        Console.WriteLine($"  score 47 -> {(grades.TryGetValue(47, out var g) ? g : "(none)")}  (TryGetValue is the safe form when a score might fall outside every band)");
    }

    /// <summary>
    /// Indexes overlapping meeting intervals, then reports which meetings a time stabs and which overlap a window.
    /// </summary>
    private static void RunIntervalTree()
    {
        // An interval tree stores overlapping [low, high] intervals and answers stabbing/overlap queries.
        var meetings = new IntervalTree<int, string>();
        meetings.Add(9, 11, "standup");
        meetings.Add(10, 12, "design");
        meetings.Add(13, 14, "lunch");
        meetings.Add(11, 15, "review");

        // QueryPoint(10) returns every interval whose span contains the point 10 - order is unspecified, so sort.
        var atTen = meetings.QueryPoint(10).Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal);
        Console.WriteLine($"  active at 10:00       : {string.Join(", ", atTen)}  (point stabbing - two meetings overlap this instant, which a coalescing range set would have merged into one)");

        // QueryOverlaps(12, 13) returns intervals overlapping the window [12,13].
        var window = meetings.QueryOverlaps(12, 13).Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal);
        Console.WriteLine($"  overlapping [12..13]  : {string.Join(", ", window)}  (a window query returns every interval touching it, however they overlap each other)");
    }
}

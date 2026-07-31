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
public static class RangesAndIntervalTree
{
    /// <summary>
    /// Coalesces ranges in a set and dictionary, then stabs and overlap-queries an interval tree.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- RangeSet / RangeDictionary / IntervalTree ---");

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
        Console.WriteLine($"  coalesced ranges : {string.Join(", ", ranges)}");

        // 15 falls inside the coalesced [0,20); 25 sits in the uncovered gap before [30,40).
        Console.WriteLine($"  contains 15 / 25 : {set.Contains(15)} / {set.Contains(25)}");
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

        Console.WriteLine($"  score 60 -> {grades[60]}");
        Console.WriteLine($"  score 90 -> {grades[90]}");

        // TryGetValue is the non-throwing lookup for keys that may not be covered by any range; the
        // indexer above would throw for an uncovered key.
        Console.WriteLine($"  score 47 -> {(grades.TryGetValue(47, out var g) ? g : "(none)")}");
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
        Console.WriteLine($"  active at 10:00       : {string.Join(", ", atTen)}");

        // QueryOverlaps(12, 13) returns intervals overlapping the window [12,13].
        var window = meetings.QueryOverlaps(12, 13).Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal);
        Console.WriteLine($"  overlapping [12..13]  : {string.Join(", ", window)}");
    }
}

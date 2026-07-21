// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervals.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Intervals.Scenarios;

/// <summary>
/// Demonstrates <see cref="DiscreteInterval{T}" /> over the integers: because the domain is
/// countable, an interval has a first and last member, an exact <c>Count</c>, and — crucially —
/// <em>adjacent</em> intervals with no gap between them merge into one, unlike the continuous
/// <see cref="Interval{T}" />.
/// </summary>
public static class DiscreteIntervals
{
    /// <summary>
    /// Enumerates the members of a discrete interval and merges two adjacent ranges.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- DiscreteInterval<T>: countable ranges ---");

        // A discrete interval over the integers has concrete endpoints and an exact member count.
        var range = DiscreteInterval<int>.Closed(3, 8);   // {3, 4, 5, 6, 7, 8}
        Console.WriteLine($"range        : {range} (First={range.First}, Last={range.Last}, Count={range.Count})");

        // The domain is countable, so we can walk every member from First to Last.
        var members = new List<int>();
        for (var value = range.First; value <= range.Last; value++)
        {
            members.Add(value);
        }

        Console.WriteLine($"members      : {string.Join(", ", members)}");

        // Adjacency is the discrete-only feature: [1,5] and [6,10] have no integer between them,
        // so TryUnion fuses them into a single interval - impossible for continuous intervals.
        var left = DiscreteInterval<int>.Closed(1, 5);
        var right = DiscreteInterval<int>.Closed(6, 10);
        var merged = left.TryUnion(right, out var union);
        Console.WriteLine($"[1,5] u [6,10] merged : {merged} -> {union}");

        // With a gap (a missing 6) the two stay separate and TryUnion reports false.
        var gapped = DiscreteInterval<int>.Closed(7, 10);
        Console.WriteLine($"[1,5] u [7,10] merged : {left.TryUnion(gapped, out _)}");

        // Difference can leave two pieces, carried by DiscreteIntervalPair<T>.
        var whole = DiscreteInterval<int>.Closed(1, 10);
        var hole = DiscreteInterval<int>.Closed(4, 6);
        DiscreteIntervalPair<int> pieces = whole.Difference(hole);
        Console.WriteLine($"[1,10] minus [4,6]    : {pieces} (Count={pieces.Count})");

        Console.WriteLine();
    }
}

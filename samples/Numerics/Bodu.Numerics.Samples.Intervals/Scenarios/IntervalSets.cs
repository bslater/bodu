// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Intervals.Scenarios;

/// <summary>
/// Demonstrates <see cref="IntervalSet{T}" />: a normalized union of disjoint intervals. Overlapping
/// or touching pieces coalesce automatically, membership is a single query across the whole set, and
/// the set-algebra operators (<c>Union</c>, <c>Intersect</c>, <c>Except</c>, <c>Complement</c>)
/// return new normalized sets.
/// </summary>
public static class IntervalSets
{
    /// <summary>
    /// Builds a set from overlapping ranges and exercises the set-algebra operators.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "IntervalSet<T> - normalized unions",
            what: "Builds a set from overlapping and adjacent spans, then unions, intersects, subtracts and " +
                  "complements it.",
            why: "A set keeps itself normalized: overlapping and touching pieces are coalesced on every " +
                 "operation, so the representation is canonical and two sets covering the same values are equal " +
                 "regardless of how they were built. A plain list of intervals gives none of that - it grows " +
                 "fragments with each edit, and answering Contains means scanning all of them. The complement is " +
                 "the operation that needs the type most, since it has to invent unbounded pieces at both ends.",
            expect: "The input spans collapse into two pieces on construction. Complementing produces three, " +
                    "including the two infinite tails, which is exactly what a list-of-ranges implementation " +
                    "cannot represent without a special case at each end.");

        // Of accepts overlapping and touching pieces and normalizes them: [0,5] and [3,8] coalesce
        // into [0,8], while the disjoint [12,15] stays separate.
        var set = IntervalSet<int>.Of(
            Interval<int>.Closed(0, 5),
            Interval<int>.Closed(3, 8),
            Interval<int>.Closed(12, 15));
        Console.WriteLine($"  normalized set       : {set} (Count={set.Count})  (the input spans coalesced on construction, so the representation is canonical and equality is meaningful)");

        // Membership is one query across every disjoint piece.
        Console.WriteLine($"  Contains(6)          : {set.Contains(6)}, Contains(10): {set.Contains(10)}, Contains(13): {set.Contains(13)}  (10 falls in the gap between the two pieces; the set checks them without the caller scanning)");

        // Union folds another interval in, re-normalizing as it goes.
        var withMore = set.Union(Interval<int>.Closed(9, 12));
        Console.WriteLine($"  union [9,12]         : {withMore}  (adding a span that bridges the gap re-normalizes rather than appending a third fragment)");   // [9,12] touches [12,15] and coalesces to [9,15]

        // Intersect keeps only the overlap with a mask interval.
        var masked = set.Intersect(Interval<int>.Closed(4, 13));
        Console.WriteLine($"  intersect [4,13]     : {masked}  (clipping can leave more pieces than it started with, which is why the result is a set)");

        // Except subtracts an interval, potentially splitting a piece.
        var punched = set.Except(Interval<int>.Closed(2, 4));
        Console.WriteLine($"  except [2,4]         : {punched}  (removing an interior span splits a piece in two and opens both new edges)");

        // Complement (within the bounded pieces present) inverts the set over the reals.
        Console.WriteLine($"  complement           : {set.Complement()}  (three pieces including two infinite tails - what a plain list of ranges cannot represent without a special case at each end)");

        Console.WriteLine();
    }
}

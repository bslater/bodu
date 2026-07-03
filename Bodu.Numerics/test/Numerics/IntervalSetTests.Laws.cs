// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.Laws.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    // Sample integer points spanning and overflowing the endpoint universe {0..6}, so membership is probed inside,
    // at the edges, and outside every piece.
    private static readonly int[] SamplePoints = { -1, 0, 1, 2, 3, 4, 5, 6, 7 };

    /// <summary>
    /// Enumerates a representative universe of interval sets over <see cref="int" /> endpoints — empty, single-piece,
    /// disconnected, and unbounded shapes.
    /// </summary>
    /// <returns>The set universe.</returns>
    private static IEnumerable<IntervalSet<int>> SetUniverse()
    {
        yield return IntervalSet<int>.Empty;
        yield return IntervalSet<int>.Of(Interval<int>.Closed(1, 3));
        yield return IntervalSet<int>.Of(Interval<int>.Closed(2, 5));
        yield return IntervalSet<int>.Of(Interval<int>.Closed(1, 2), Interval<int>.Closed(4, 5));
        yield return IntervalSet<int>.Of(Interval<int>.ClosedOpen(0, 3), Interval<int>.Closed(4, 6));
        yield return IntervalSet<int>.Of(Interval<int>.All);
        yield return IntervalSet<int>.Of(Interval<int>.AtMost(2));
        yield return IntervalSet<int>.Of(Interval<int>.AtLeast(3));
    }

    /// <summary>
    /// Verifies, exhaustively over the set universe and a sample point grid, that
    /// <see cref="IntervalSet{T}.Union(IntervalSet{T})" />, <see cref="IntervalSet{T}.Intersect(IntervalSet{T})" />, and
    /// <see cref="IntervalSet{T}.Except(IntervalSet{T})" /> agree with the pointwise set predicates they implement.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SetOperations_OverFiniteUniverse_ShouldSatisfyMembershipLaws()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            foreach (IntervalSet<int> b in universe)
            {
                IntervalSet<int> union = a.Union(b);
                IntervalSet<int> intersection = a.Intersect(b);
                IntervalSet<int> difference = a.Except(b);

                foreach (int x in SamplePoints)
                {
                    bool inA = a.Contains(x);
                    bool inB = b.Contains(x);

                    Assert.AreEqual(inA || inB, union.Contains(x), $"Union: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA && inB, intersection.Contains(x), $"Intersect: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA && !inB, difference.Contains(x), $"Except: a={a}, b={b}, x={x}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Complement" /> is the pointwise negation of membership over the line, and
    /// that applying it twice restores the original set.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Complement_OverFiniteUniverse_ShouldBePointwiseNegation()
    {
        foreach (IntervalSet<int> set in SetUniverse())
        {
            IntervalSet<int> complement = set.Complement();

            foreach (int x in SamplePoints)
                Assert.AreEqual(!set.Contains(x), complement.Contains(x), $"Complement: set={set}, x={x}");

            Assert.AreEqual(set, complement.Complement(), $"Double complement: set={set}");
        }
    }
}

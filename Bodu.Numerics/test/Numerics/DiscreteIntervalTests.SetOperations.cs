// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies membership and count for a bounded interval.
    /// </summary>
    [TestMethod]
    public void ContainsAndCount_WhenBounded_ShouldReflectIntegerMembers()
    {
        DiscreteInterval<int> value = DiscreteInterval<int>.Closed(1, 10);

        Assert.AreEqual(10, value.Count);
        Assert.IsTrue(value.Contains(1));
        Assert.IsTrue(value.Contains(10));
        Assert.IsFalse(value.Contains(0));
        Assert.IsFalse(value.Contains(11));
    }

    /// <summary>
    /// Verifies that reading <see cref="DiscreteInterval{T}.Count" /> on an unbounded interval throws
    /// <see cref="InvalidOperationException" />, and that membership extends to infinity.
    /// </summary>
    [TestMethod]
    public void Unbounded_WhenTested_ShouldExtendToInfinityAndRejectCount()
    {
        Assert.IsTrue(DiscreteInterval<int>.AtLeast(5).Contains(1_000_000));
        Assert.IsFalse(DiscreteInterval<int>.AtLeast(5).Contains(4));
        Assert.IsTrue(DiscreteInterval<int>.All.Contains(int.MinValue));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DiscreteInterval<int>.AtLeast(5).Count;
        });
    }

    /// <summary>
    /// Verifies intersection over bounded and unbounded operands.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsOverlap_ShouldReturnSharedIntegers()
    {
        Assert.AreEqual(DiscreteInterval<int>.Closed(5, 10), DiscreteInterval<int>.Closed(1, 10).Intersect(DiscreteInterval<int>.Closed(5, 15)));
        Assert.IsTrue(DiscreteInterval<int>.Closed(1, 3).Intersect(DiscreteInterval<int>.Closed(5, 7)).IsEmpty);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 10), DiscreteInterval<int>.AtLeast(0).Intersect(DiscreteInterval<int>.AtMost(10)));
        Assert.AreEqual(DiscreteInterval<int>.Closed(3, 5), DiscreteInterval<int>.All.Intersect(DiscreteInterval<int>.Closed(3, 5)));
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Overlaps(DiscreteInterval{T})" /> reports a shared integer, and that
    /// successor-adjacent runs (which share no integer) do not overlap.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenSharingAnInteger_ShouldReturnTrue()
    {
        Assert.IsTrue(DiscreteInterval<int>.Closed(1, 5).Overlaps(DiscreteInterval<int>.Closed(3, 7)));
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 2).Overlaps(DiscreteInterval<int>.Closed(3, 4)));
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 5).Overlaps(DiscreteInterval<int>.Empty));
        Assert.IsTrue(DiscreteInterval<int>.AtLeast(0).Overlaps(DiscreteInterval<int>.Closed(5, 10)));
    }

    /// <summary>
    /// Verifies that overlapping intervals union into their span.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOverlapping_ShouldReturnSpan()
    {
        bool ok = DiscreteInterval<int>.Closed(1, 5).TryUnion(DiscreteInterval<int>.Closed(3, 9), out DiscreteInterval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(DiscreteInterval<int>.Closed(1, 9), result);
    }

    // The integer universe for the exhaustive membership harness.
    private static readonly int[] SampleIntegers = { -1, 0, 1, 2, 3, 4, 5, 6 };

    private static IEnumerable<DiscreteInterval<int>> DiscreteUniverse()
    {
        for (int lower = 0; lower <= 4; lower++)
        {
            for (int upper = 0; upper <= 4; upper++)
            {
                yield return DiscreteInterval<int>.Closed(lower, upper);
                yield return DiscreteInterval<int>.Open(lower, upper);
                yield return DiscreteInterval<int>.ClosedOpen(lower, upper);
            }
        }

        yield return DiscreteInterval<int>.All;
        yield return DiscreteInterval<int>.AtLeast(2);
        yield return DiscreteInterval<int>.AtMost(2);
        yield return DiscreteInterval<int>.Empty;
    }

    /// <summary>
    /// Verifies that two intervals meeting the opposite ends of the integer domain are not treated as
    /// successor-adjacent: the successor computation of <c>T.MaxValue</c> must not wrap to <c>T.MinValue</c> and merge
    /// two runs that are separated by a real gap.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenRunsTouchOppositeDomainExtremes_ShouldNotWrapIntoAdjacency()
    {
        DiscreteInterval<int> high = DiscreteInterval<int>.Closed(5, int.MaxValue);
        DiscreteInterval<int> low = DiscreteInterval<int>.Closed(int.MinValue, 3);

        Assert.IsFalse(high.TryUnion(low, out _));
        Assert.IsFalse(low.TryUnion(high, out _));
    }

    /// <summary>
    /// Verifies, exhaustively over an integer universe, that intersection membership equals the pointwise integer
    /// predicate and that a contiguous union covers exactly the union of members.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SetOperations_OverIntegerDomain_ShouldSatisfyMembershipLaws()
    {
        DiscreteInterval<int>[] universe = DiscreteUniverse().ToArray();

        foreach (DiscreteInterval<int> a in universe)
        {
            foreach (DiscreteInterval<int> b in universe)
            {
                DiscreteInterval<int> intersection = a.Intersect(b);
                bool contiguous = a.TryUnion(b, out DiscreteInterval<int> union);

                foreach (int x in SampleIntegers)
                {
                    bool inA = a.Contains(x);
                    bool inB = b.Contains(x);

                    Assert.AreEqual(inA && inB, intersection.Contains(x), $"Intersect: a={a}, b={b}, x={x}");

                    if (contiguous)
                        Assert.AreEqual(inA || inB, union.Contains(x), $"Union: a={a}, b={b}, x={x}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a discrete interval round-trips through its continuous form and back.
    /// </summary>
    [TestMethod]
    public void ToInterval_AndBack_ShouldRoundTrip()
    {
        DiscreteInterval<int> value = DiscreteInterval<int>.Closed(2, 8);

        Assert.AreEqual(Interval<int>.Closed(2, 8), value.ToInterval());
        Assert.AreEqual(value, DiscreteInterval<int>.FromInterval(Interval<int>.Closed(2, 8)));
        Assert.AreEqual(value, DiscreteInterval<int>.FromInterval(Interval<int>.Open(1, 9)));
    }
}

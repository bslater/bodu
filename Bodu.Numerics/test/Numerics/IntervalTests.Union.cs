// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Union.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the union of two overlapping intervals takes the smaller lower endpoint and the larger upper.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOverlapping_ShouldReturnSpanningInterval()
    {
        var ok = Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Closed(3, 7), out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.Closed(1, 7), result);
    }

    /// <summary>
    /// Verifies that adjacent intervals — where one's upper endpoint equals the other's lower endpoint and at least
    /// one is inclusive — union to a contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenAdjacent_ShouldReturnContiguousInterval()
    {
        // [1, 5) ∪ [5, 10] -> [1, 10]
        var ok = Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.Closed(5, 10), out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.Closed(1, 10), result);
    }

    /// <summary>
    /// Verifies that two intervals with a gap at the touching point — both open at the shared endpoint — cannot be
    /// unioned to a single contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenBothOpenAtSharedEndpoint_ShouldReturnFalse()
    {
        // [1, 5) ∪ (5, 10] — neither contains 5, so the result is not contiguous.
        var ok = Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.OpenClosed(5, 10), out Interval<int> result);

        Assert.IsFalse(ok);
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that disjoint intervals separated by a gap cannot be unioned to a single contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenDisjoint_ShouldReturnFalse()
    {
        var ok = Interval<int>.Closed(1, 3).TryUnion(Interval<int>.Closed(5, 7), out Interval<int> result);

        Assert.IsFalse(ok);
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that the union of any interval with the empty interval returns the original interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenEitherOperandIsEmpty_ShouldReturnTheOther()
    {
        var nonEmpty = Interval<int>.Closed(1, 5);

        Assert.IsTrue(Interval<int>.Empty.TryUnion(nonEmpty, out Interval<int> a));
        Assert.AreEqual(nonEmpty, a);

        Assert.IsTrue(nonEmpty.TryUnion(Interval<int>.Empty, out Interval<int> b));
        Assert.AreEqual(nonEmpty, b);
    }

    /// <summary>
    /// Verifies that when the two operands' endpoints tie, union picks the looser (inclusive) inclusivity.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenEndpointsTie_ShouldPickLooserInclusivity()
    {
        // [1, 5] ∪ (1, 5) -> [1, 5]
        Assert.IsTrue(Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Open(1, 5), out Interval<int> result));
        Assert.AreEqual(Interval<int>.Closed(1, 5), result);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Intersect.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that <see cref="Interval{T}.Overlaps" /> returns true for any pair of intervals that share a value.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenIntervalsShareAValue_ShouldReturnTrue()
    {
        Assert.IsTrue(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Closed(3, 7)));
        Assert.IsTrue(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Closed(5, 7)));
        Assert.IsTrue(Interval<int>.Closed(1, 10).Overlaps(Interval<int>.Closed(3, 7)));
    }

    /// <summary>
    /// Verifies that two intervals that touch at a value not included in either (or only one) do not overlap.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenIntervalsOnlyTouch_ShouldReflectInclusivity()
    {
        // [1, 5) and [5, 10] share no value because 5 is excluded from the first.
        Assert.IsFalse(Interval<int>.ClosedOpen(1, 5).Overlaps(Interval<int>.Closed(5, 10)));

        // (1, 5] and [5, 10] share the value 5.
        Assert.IsTrue(Interval<int>.OpenClosed(1, 5).Overlaps(Interval<int>.Closed(5, 10)));

        // (1, 5) and (5, 10) share nothing.
        Assert.IsFalse(Interval<int>.Open(1, 5).Overlaps(Interval<int>.Open(5, 10)));
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Overlaps" /> returns false when one operand is empty.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenEitherOperandIsEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(Interval<int>.Empty.Overlaps(Interval<int>.Closed(1, 5)));
        Assert.IsFalse(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Empty));
        Assert.IsFalse(Interval<int>.Empty.Overlaps(Interval<int>.Empty));
    }

    /// <summary>
    /// Verifies that the intersection of overlapping intervals takes the larger lower endpoint and the smaller upper.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOverlapping_ShouldReturnTheNarrowestSubinterval()
    {
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.Closed(3, 7)));
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.Closed(1, 10).Intersect(Interval<int>.Closed(3, 5)));
    }

    /// <summary>
    /// Verifies that inclusivity flags propagate correctly through intersection — the stricter (open) flag wins when
    /// endpoints tie.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenEndpointsTie_ShouldPickStricterInclusivity()
    {
        // [1, 5] ∩ (1, 5) -> (1, 5)
        Assert.AreEqual(Interval<int>.Open(1, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.Open(1, 5)));

        // [1, 5] ∩ [1, 5) -> [1, 5)
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.ClosedOpen(1, 5)));
    }

    /// <summary>
    /// Verifies that the intersection of disjoint intervals is empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenDisjoint_ShouldReturnEmpty()
    {
        Assert.IsTrue(Interval<int>.Closed(1, 3).Intersect(Interval<int>.Closed(5, 7)).IsEmpty);
        Assert.IsTrue(Interval<int>.ClosedOpen(1, 5).Intersect(Interval<int>.Closed(5, 7)).IsEmpty);
    }

    /// <summary>
    /// Verifies that intersection with the empty interval is always empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenEitherOperandIsEmpty_ShouldReturnEmpty()
    {
        Assert.IsTrue(Interval<int>.Empty.Intersect(Interval<int>.Closed(1, 5)).IsEmpty);
        Assert.IsTrue(Interval<int>.Closed(1, 5).Intersect(Interval<int>.Empty).IsEmpty);
    }

    /// <summary>
    /// Verifies that intersecting an interval whose lower bound starts after the other operand keeps this interval's
    /// lower endpoint and the other operand's upper endpoint.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenThisStartsAfterOther_ShouldClampToThisLowerAndOtherUpper()
    {
        var result = Interval<int>.Closed(3, 8).Intersect(Interval<int>.Closed(1, 5));

        Assert.AreEqual(Interval<int>.Closed(3, 5), result);
    }
}

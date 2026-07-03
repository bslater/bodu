// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    /// <summary>
    /// Verifies the complement example from Python's <c>portion</c> library: <c>~P.closed(0, 5) == (-inf, 0) | (5, +inf)</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Complement_ForPortionReferenceVector_ShouldMatchOpenTails()
    {
        IntervalSet<int> complement = IntervalSet<int>.Of(Interval<int>.Closed(0, 5)).Complement();

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.LessThan(0), Interval<int>.GreaterThan(5)),
            complement);
    }

    /// <summary>
    /// Verifies the adjacent-then-fill merge that Guava's <c>RangeSet</c> performs: adding <c>[1, 4]</c>, <c>[6, 8]</c>,
    /// then the bridging <c>[4, 6]</c> coalesces into the single connected range <c>[1, 8]</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Of_ForGuavaRangeSetBridgeVector_ShouldCoalesceToSingleRange()
    {
        var set = IntervalSet<int>.Of(
            Interval<int>.Closed(1, 4),
            Interval<int>.Closed(6, 8),
            Interval<int>.Closed(4, 6));

        Assert.AreEqual(1, set.Count);
        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 8)), set);
    }

    /// <summary>
    /// Verifies the difference example from Python's <c>portion</c> library:
    /// <c>P.closed(0, 10) - P.closed(3, 5) == [0, 3) | (5, 10]</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Except_ForPortionDifferenceVector_ShouldSplitAroundRemovedRange()
    {
        IntervalSet<int> result = IntervalSet<int>.Of(Interval<int>.Closed(0, 10))
            .Except(IntervalSet<int>.Of(Interval<int>.Closed(3, 5)));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.ClosedOpen(0, 3), Interval<int>.OpenClosed(5, 10)),
            result);
    }

    /// <summary>
    /// Verifies the touching-closed union that <c>portion</c> simplifies: <c>P.closed(1, 2) | P.closed(2, 3) == [1, 3]</c>
    /// — overlapping at the shared endpoint, the two ranges coalesce into one.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Union_ForPortionTouchingClosedVector_ShouldCoalesce()
    {
        IntervalSet<int> result = IntervalSet<int>.Of(Interval<int>.Closed(1, 2))
            .Union(IntervalSet<int>.Of(Interval<int>.Closed(2, 3)));

        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 3)), result);
    }
}

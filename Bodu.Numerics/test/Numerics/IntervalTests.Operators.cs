// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the <c>&amp;</c> operator computes the intersection.
    /// </summary>
    [TestMethod]
    public void OperatorAnd_WhenIntervalsOverlap_ShouldReturnIntersection()
    {
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.Closed(1, 5) & Interval<int>.Closed(3, 7));
    }

    /// <summary>
    /// Verifies that the <c>&amp;</c> operator returns the empty interval for disjoint operands.
    /// </summary>
    [TestMethod]
    public void OperatorAnd_WhenIntervalsDisjoint_ShouldReturnEmpty()
    {
        Assert.IsTrue((Interval<int>.Closed(1, 3) & Interval<int>.Closed(5, 7)).IsEmpty);
    }

    /// <summary>
    /// Verifies that the <c>|</c> operator computes the contiguous union of adjacent operands.
    /// </summary>
    [TestMethod]
    public void OperatorOr_WhenIntervalsAdjacent_ShouldReturnContiguousUnion()
    {
        Assert.AreEqual(Interval<int>.Closed(1, 10), Interval<int>.ClosedOpen(1, 5) | Interval<int>.Closed(5, 10));
    }

    /// <summary>
    /// Verifies that the <c>|</c> operator throws <see cref="InvalidOperationException" /> for disjoint, non-adjacent
    /// operands whose union is not a single interval.
    /// </summary>
    [TestMethod]
    public void OperatorOr_WhenIntervalsHaveAGap_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Interval<int>.Closed(1, 2) | Interval<int>.Closed(5, 6);
        });
    }
}

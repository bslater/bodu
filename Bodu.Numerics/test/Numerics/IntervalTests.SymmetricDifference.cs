// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.SymmetricDifference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the symmetric difference of two partially overlapping intervals is the two non-shared flanks.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenPartiallyOverlapping_ShouldReturnBothFlanks()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(3, 8));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), result[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 8), result[1]);
    }

    /// <summary>
    /// Verifies that the symmetric difference of equal intervals is empty.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenEqual_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(0, 5)).Count);
    }

    /// <summary>
    /// Verifies that the symmetric difference with an interior interval matches the plain difference (the interior
    /// contributes nothing of its own).
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOtherIsInterior_ShouldMatchDifference()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 10).SymmetricDifference(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), result[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), result[1]);
    }

    /// <summary>
    /// Verifies that the symmetric difference of two disjoint intervals is both intervals, ordered lowest-first.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenDisjoint_ShouldReturnBothOrdered()
    {
        IntervalPair<int> result = Interval<int>.Closed(5, 7).SymmetricDifference(Interval<int>.Closed(0, 2));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(Interval<int>.Closed(5, 7), result[1]);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.QueryPoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that a point before every stored interval yields no matches.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointBeforeAllIntervals_ShouldYieldNothing()
    {
        var sut = CreateTree((10, 20), (15, 25));

        Assert.AreEqual(0, sut.QueryPoint(5).Count());
    }

    /// <summary>
    /// Verifies that a point after every stored interval yields no matches.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointAfterAllIntervals_ShouldYieldNothing()
    {
        var sut = CreateTree((10, 20), (15, 25));

        Assert.AreEqual(0, sut.QueryPoint(30).Count());
    }

    /// <summary>
    /// Verifies that a point strictly inside an interval yields that interval.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointInsideInterval_ShouldYieldInterval()
    {
        var sut = CreateTree((10, 20));

        CollectionAssert.AreEqual(new[] { (10, 20) }, sut.QueryPoint(15).ToList());
    }

    /// <summary>
    /// Verifies that both closed endpoints are inclusive — stabbing at the low or the high endpoint yields the
    /// interval.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointAtEndpoints_ShouldYieldIntervalInclusively()
    {
        var sut = CreateTree((10, 20));

        CollectionAssert.AreEqual(new[] { (10, 20) }, sut.QueryPoint(10).ToList());
        CollectionAssert.AreEqual(new[] { (10, 20) }, sut.QueryPoint(20).ToList());
        Assert.AreEqual(0, sut.QueryPoint(9).Count());
        Assert.AreEqual(0, sut.QueryPoint(21).Count());
    }

    /// <summary>
    /// Verifies that nested and overlapping intervals containing the point are all yielded, in ascending
    /// (low, high) order.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenIntervalsNestAndOverlap_ShouldYieldAllContainingInAscendingOrder()
    {
        var sut = CreateTree((1, 100), (40, 60), (45, 55), (50, 50), (10, 20), (70, 80));

        CollectionAssert.AreEqual(
            new[] { (1, 100), (40, 60), (45, 55), (50, 50) }, sut.QueryPoint(50).ToList());
    }

    /// <summary>
    /// Verifies that a duplicated interval containing the point is repeated once per stored occurrence.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenIntervalDuplicated_ShouldRepeatPerOccurrence()
    {
        var sut = CreateTree((10, 20), (10, 20), (12, 18));

        CollectionAssert.AreEqual(new[] { (10, 20), (10, 20), (12, 18) }, sut.QueryPoint(15).ToList());
    }

    /// <summary>
    /// Verifies that a degenerate single-point interval is yielded only when stabbed exactly at its point.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenIntervalDegenerate_ShouldMatchOnlyItsPoint()
    {
        var sut = CreateTree((5, 5));

        CollectionAssert.AreEqual(new[] { (5, 5) }, sut.QueryPoint(5).ToList());
        Assert.AreEqual(0, sut.QueryPoint(4).Count());
        Assert.AreEqual(0, sut.QueryPoint(6).Count());
    }

    /// <summary>
    /// Verifies that querying an empty tree yields nothing.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenTreeEmpty_ShouldYieldNothing()
    {
        var sut = new IntervalTree<int>();

        Assert.AreEqual(0, sut.QueryPoint(5).Count());
    }

    /// <summary>
    /// Verifies that the lazy stabbing sequence is fail-fast — mutating the tree mid-iteration throws
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenTreeMutatedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 10), (2, 9), (3, 8));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High) _ in sut.QueryPoint(5))
                sut.Add(4, 7);
        });
    }

    /// <summary>
    /// Verifies that the sequence is live — a fresh iteration reflects mutations made after the query was obtained.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenIteratedAfterMutation_ShouldReflectCurrentState()
    {
        var sut = CreateTree((1, 10));
        IEnumerable<(int Low, int High)> query = sut.QueryPoint(5);

        sut.Add(2, 8);

        CollectionAssert.AreEqual(new[] { (1, 10), (2, 8) }, query.ToList());
    }

    /// <summary>
    /// Verifies that stabbing with a <see langword="null" /> point throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.QueryPoint(null!);
        });
    }
}

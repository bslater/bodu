// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.QueryOverlaps.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies the window-relation matrix against a single stored interval [10, 20]: windows disjoint on either
    /// side miss; windows touching, contained, containing, or straddling either edge match (closed semantics).
    /// </summary>
    [TestMethod]
    [DataRow("disjoint-left", 1, 5, false)]
    [DataRow("touching-left", 5, 10, true)]
    [DataRow("straddling-left", 5, 12, true)]
    [DataRow("contained", 12, 15, true)]
    [DataRow("equal", 10, 20, true)]
    [DataRow("containing", 5, 25, true)]
    [DataRow("straddling-right", 18, 25, true)]
    [DataRow("touching-right", 20, 25, true)]
    [DataRow("disjoint-right", 25, 30, false)]
    public void QueryOverlaps_WhenWindowRelationVaries_ShouldMatchClosedIntervalSemantics(
        string testName, int windowLow, int windowHigh, bool expectedMatch)
    {
        var sut = CreateTree((10, 20));

        List<(int Low, int High)> matches = sut.QueryOverlaps(windowLow, windowHigh).ToList();

        Assert.AreEqual(expectedMatch ? 1 : 0, matches.Count, testName);
    }

    /// <summary>
    /// Verifies that all intervals overlapping the window are yielded in ascending (low, high) order while
    /// non-overlapping intervals are excluded.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenMultipleIntervalsStored_ShouldYieldOverlappingInAscendingOrder()
    {
        var sut = CreateTree((30, 40), (1, 5), (8, 12), (11, 25), (26, 28), (50, 60));

        CollectionAssert.AreEqual(
            new[] { (8, 12), (11, 25), (26, 28), (30, 40) }, sut.QueryOverlaps(10, 30).ToList());
    }

    /// <summary>
    /// Verifies that a duplicated overlapping interval is repeated once per stored occurrence.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenIntervalDuplicated_ShouldRepeatPerOccurrence()
    {
        var sut = CreateTree((10, 20), (10, 20));

        CollectionAssert.AreEqual(new[] { (10, 20), (10, 20) }, sut.QueryOverlaps(15, 30).ToList());
    }

    /// <summary>
    /// Verifies that a degenerate window whose edges are equal behaves as a stabbing query.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenWindowDegenerate_ShouldBehaveAsStab()
    {
        var sut = CreateTree((10, 20), (15, 25), (21, 30));

        CollectionAssert.AreEqual(new[] { (10, 20), (15, 25) }, sut.QueryOverlaps(18, 18).ToList());
    }

    /// <summary>
    /// Verifies that querying an empty tree yields nothing.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenTreeEmpty_ShouldYieldNothing()
    {
        var sut = new IntervalTree<int>();

        Assert.AreEqual(0, sut.QueryOverlaps(1, 100).Count());
    }

    /// <summary>
    /// Verifies that the lazy overlap sequence is fail-fast — mutating the tree mid-iteration throws
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenTreeMutatedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 10), (2, 9), (3, 8));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High) _ in sut.QueryOverlaps(0, 20))
                sut.Remove(2, 9);
        });
    }

    /// <summary>
    /// Verifies that querying with a window whose lower edge orders after its upper edge throws
    /// <see cref="ArgumentException" /> eagerly, before any iteration.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenLowExceedsHigh_ShouldThrowArgumentException()
    {
        var sut = CreateTree((1, 5));

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.QueryOverlaps(10, 5);
        });

        Assert.AreEqual("low", ex.ParamName);
    }

    /// <summary>
    /// Verifies that querying with a <see langword="null" /> window edge throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenEdgeIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.QueryOverlaps(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.QueryOverlaps("a", null!);
        });
    }
}

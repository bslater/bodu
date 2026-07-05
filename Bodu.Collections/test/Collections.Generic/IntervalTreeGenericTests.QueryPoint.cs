// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.QueryPoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies that stabbing yields the containing entries with their values, ascending by (low, high), and
    /// excludes entries not containing the point.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenEntriesOverlap_ShouldYieldContainingEntriesWithValues()
    {
        var sut = CreateTree((1, 100, "outer"), (40, 60, "middle"), (45, 55, "inner"), (70, 80, "aside"));

        CollectionAssert.AreEqual(
            new[] { (1, 100, "outer"), (40, 60, "middle"), (45, 55, "inner") }, sut.QueryPoint(50).ToList());
    }

    /// <summary>
    /// Verifies that an interval carrying multiple values yields one entry per value, in insertion order.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenIntervalCarriesMultipleValues_ShouldYieldOneEntryPerValue()
    {
        var sut = CreateTree((10, 12, "design review"), (10, 12, "1:1"), (11, 13, "later"));

        CollectionAssert.AreEqual(
            new[] { (10, 12, "design review"), (10, 12, "1:1"), (11, 13, "later") }, sut.QueryPoint(11).ToList());
    }

    /// <summary>
    /// Verifies that both closed endpoints are inclusive and points outside every entry yield nothing.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointAtOrOutsideEndpoints_ShouldMatchClosedSemantics()
    {
        var sut = CreateTree((10, 20, "x"));

        CollectionAssert.AreEqual(new[] { (10, 20, "x") }, sut.QueryPoint(10).ToList());
        CollectionAssert.AreEqual(new[] { (10, 20, "x") }, sut.QueryPoint(20).ToList());
        Assert.AreEqual(0, sut.QueryPoint(9).Count());
        Assert.AreEqual(0, sut.QueryPoint(21).Count());
    }

    /// <summary>
    /// Verifies that the lazy stabbing sequence is fail-fast — mutating the tree mid-iteration throws
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenTreeMutatedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 10, "a"), (2, 9, "b"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High, string Value) _ in sut.QueryPoint(5))
                sut.Add(3, 8, "c");
        });
    }

    /// <summary>
    /// Verifies that stabbing with a <see langword="null" /> point throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void QueryPoint_WhenPointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.QueryPoint(null!);
        });
    }
}

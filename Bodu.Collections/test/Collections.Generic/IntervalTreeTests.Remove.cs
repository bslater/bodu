// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that removing a stored interval returns <see langword="true" /> and removes it from the tree.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalStored_ShouldRemoveAndReturnTrue()
    {
        var sut = CreateTree((1, 5), (10, 15));

        Assert.IsTrue(sut.Remove(1, 5));

        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.Contains(1, 5));
    }

    /// <summary>
    /// Verifies that removing an interval that is not stored returns <see langword="false" /> and leaves the tree
    /// untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalAbsent_ShouldReturnFalse()
    {
        var sut = CreateTree((1, 5));

        Assert.IsFalse(sut.Remove(2, 4));

        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that removal requires an exact (low, high) match — an interval overlapping the arguments but not
    /// equal to them is not removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalMerelyOverlaps_ShouldReturnFalse()
    {
        var sut = CreateTree((1, 10));

        Assert.IsFalse(sut.Remove(1, 9));
        Assert.IsFalse(sut.Remove(2, 10));

        Assert.IsTrue(sut.Contains(1, 10));
    }

    /// <summary>
    /// Verifies that removing a duplicated interval removes one occurrence per call, keeping the remaining
    /// occurrences stored until the last one is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalDuplicated_ShouldRemoveOneOccurrencePerCall()
    {
        var sut = CreateTree((1, 5), (1, 5), (1, 5));

        Assert.IsTrue(sut.Remove(1, 5));
        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains(1, 5));

        Assert.IsTrue(sut.Remove(1, 5));
        Assert.IsTrue(sut.Remove(1, 5));
        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains(1, 5));

        Assert.IsFalse(sut.Remove(1, 5));
    }

    /// <summary>
    /// Verifies that removing the root of a single-interval tree empties the tree.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTreeHoldsSingleInterval_ShouldEmptyTree()
    {
        var sut = CreateTree((3, 7));

        Assert.IsTrue(sut.Remove(3, 7));

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.ToList().Count);
    }

    /// <summary>
    /// Verifies the targeted node-delete shapes — leaf, node with one child, and interior node with two children
    /// (reduced via its in-order successor) — by removing from a tree covering all shapes and checking the
    /// survivors' order and query behaviour.
    /// </summary>
    [TestMethod]
    public void Remove_WhenNodeShapesVary_ShouldPreserveOrderAndQueries()
    {
        var sut = CreateTree((40, 45), (20, 60), (60, 65), (10, 30), (30, 35), (50, 90), (70, 75));

        // Interior node with two children.
        Assert.IsTrue(sut.Remove(40, 45));
        CollectionAssert.AreEqual(
            new[] { (10, 30), (20, 60), (30, 35), (50, 90), (60, 65), (70, 75) }, sut.ToList());

        // Leaf node.
        Assert.IsTrue(sut.Remove(10, 30));
        CollectionAssert.AreEqual(new[] { (20, 60), (30, 35), (50, 90), (60, 65), (70, 75) }, sut.ToList());

        // Remove down to a small tree, exercising one-child splices along the way.
        Assert.IsTrue(sut.Remove(60, 65));
        Assert.IsTrue(sut.Remove(20, 60));
        CollectionAssert.AreEqual(new[] { (30, 35), (50, 90), (70, 75) }, sut.ToList());

        // The Max augmentation must survive every delete shape: [20, 60] no longer answers stabs.
        Assert.IsFalse(sut.IntersectsPoint(40));
        CollectionAssert.AreEqual(new[] { (50, 90) }, sut.QueryPoint(55).ToList());
    }

    /// <summary>
    /// Verifies that removing an interval whose lower endpoint orders after its upper endpoint throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLowExceedsHigh_ShouldThrowArgumentException()
    {
        var sut = CreateTree((1, 5));

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Remove(5, 1);
        });

        Assert.AreEqual("low", ex.ParamName);
    }

    /// <summary>
    /// Verifies that removing with a <see langword="null" /> endpoint throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Remove(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Remove("a", null!);
        });
    }
}

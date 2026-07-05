// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Intersects.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.IntersectsPoint" /> reports containment inclusively at both closed
    /// endpoints and misses just outside them.
    /// </summary>
    [TestMethod]
    public void IntersectsPoint_WhenPointAtOrOutsideEndpoints_ShouldMatchClosedSemantics()
    {
        var sut = CreateTree((10, 20), (30, 40));

        Assert.IsTrue(sut.IntersectsPoint(10));
        Assert.IsTrue(sut.IntersectsPoint(15));
        Assert.IsTrue(sut.IntersectsPoint(20));
        Assert.IsFalse(sut.IntersectsPoint(9));
        Assert.IsFalse(sut.IntersectsPoint(21));
        Assert.IsFalse(sut.IntersectsPoint(25));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.IntersectsPoint" /> returns <see langword="false" /> on an empty
    /// tree.
    /// </summary>
    [TestMethod]
    public void IntersectsPoint_WhenTreeEmpty_ShouldReturnFalse()
    {
        var sut = new IntervalTree<int>();

        Assert.IsFalse(sut.IntersectsPoint(5));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Intersects" /> reports window overlap for touching, contained,
    /// and containing windows, and misses disjoint windows on either side.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenWindowRelationVaries_ShouldMatchClosedIntervalSemantics()
    {
        var sut = CreateTree((10, 20));

        Assert.IsFalse(sut.Intersects(1, 5));
        Assert.IsTrue(sut.Intersects(5, 10));
        Assert.IsTrue(sut.Intersects(12, 15));
        Assert.IsTrue(sut.Intersects(5, 25));
        Assert.IsTrue(sut.Intersects(20, 25));
        Assert.IsFalse(sut.Intersects(25, 30));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Intersects" /> finds an overlap held only in a right subtree when
    /// the left descent cannot reach the window.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenOverlapDeepInTree_ShouldFindIt()
    {
        var sut = CreateTree((1, 2), (4, 5), (7, 8), (10, 11), (13, 14), (16, 40), (19, 20));

        Assert.IsTrue(sut.Intersects(30, 35));
        Assert.IsFalse(sut.Intersects(41, 50));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Intersects" /> throws <see cref="ArgumentException" /> for a window
    /// whose lower edge orders after its upper edge, and <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> edge.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenArgumentsInvalid_ShouldThrow()
    {
        var stringTree = new IntervalTree<string>();
        var intTree = CreateTree((1, 5));

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            intTree.Intersects(5, 1);
        });
        Assert.AreEqual("low", ex.ParamName);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stringTree.Intersects(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stringTree.IntersectsPoint(null!);
        });
    }
}

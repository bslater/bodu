// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Clear" /> removes every interval, resets the count, and empties
    /// enumeration and queries.
    /// </summary>
    [TestMethod]
    public void Clear_WhenTreePopulated_ShouldRemoveAllIntervals()
    {
        var sut = CreateTree((1, 5), (3, 8), (3, 8), (10, 12));

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.ToList().Count);
        Assert.AreEqual(0, sut.QueryOverlaps(0, 100).Count());
        Assert.IsFalse(sut.IntersectsPoint(4));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Clear" /> on an empty tree leaves it empty and usable.
    /// </summary>
    [TestMethod]
    public void Clear_WhenTreeEmpty_ShouldRemainEmptyAndUsable()
    {
        var sut = new IntervalTree<int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);

        sut.Add(1, 2);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the tree accepts new intervals normally after being cleared.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldBehaveAsFreshTree()
    {
        var sut = CreateTree((1, 5), (6, 9));

        sut.Clear();
        sut.Add(20, 30);

        Assert.AreEqual(1, sut.Count);
        CollectionAssert.AreEqual(new[] { (20, 30) }, sut.ToList());
    }
}

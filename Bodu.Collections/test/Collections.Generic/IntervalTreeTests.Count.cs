// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Count" /> is zero for a new tree.
    /// </summary>
    [TestMethod]
    public void Count_WhenTreeEmpty_ShouldReturnZero()
    {
        var sut = new IntervalTree<int>();

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Count" /> counts duplicate occurrences of the same interval
    /// individually.
    /// </summary>
    [TestMethod]
    public void Count_WhenDuplicatesStored_ShouldIncludeEveryOccurrence()
    {
        var sut = CreateTree((1, 5), (1, 5), (2, 6));

        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Count" /> tracks additions and removals, including duplicate
    /// occurrence removal.
    /// </summary>
    [TestMethod]
    public void Count_WhenMutated_ShouldTrackAddAndRemove()
    {
        var sut = new IntervalTree<int>();

        sut.Add(1, 5);
        sut.Add(1, 5);
        sut.Add(10, 20);
        Assert.AreEqual(3, sut.Count);

        sut.Remove(1, 5);
        Assert.AreEqual(2, sut.Count);

        sut.Remove(10, 20);
        Assert.AreEqual(1, sut.Count);

        sut.Remove(99, 100);
        Assert.AreEqual(1, sut.Count);
    }
}

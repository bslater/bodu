// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that adding non-overlapping intervals stores them all and enumerates them in ascending order.
    /// </summary>
    [TestMethod]
    public void Add_WhenIntervalsDisjoint_ShouldStoreAll()
    {
        var sut = CreateTree((20, 30), (1, 5), (10, 15));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { (1, 5), (10, 15), (20, 30) }, sut.ToList());
    }

    /// <summary>
    /// Verifies that overlapping and nested intervals are all stored rather than merged or rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenIntervalsOverlap_ShouldStoreAllWithoutMerging()
    {
        var sut = CreateTree((1, 10), (5, 15), (7, 8));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { (1, 10), (5, 15), (7, 8) }, sut.ToList());
    }

    /// <summary>
    /// Verifies that adding an interval equal to one already stored records a duplicate occurrence, growing
    /// <see cref="IntervalTree{T}.Count" /> and repeating the interval in enumeration.
    /// </summary>
    [TestMethod]
    public void Add_WhenIntervalIsDuplicate_ShouldRecordAdditionalOccurrence()
    {
        var sut = CreateTree((1, 5), (1, 5), (1, 5));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { (1, 5), (1, 5), (1, 5) }, sut.ToList());
    }

    /// <summary>
    /// Verifies that intervals sharing a low endpoint but differing in high endpoint are stored as distinct
    /// intervals ordered by the (low, high) pair.
    /// </summary>
    [TestMethod]
    public void Add_WhenIntervalsShareLowEndpoint_ShouldStoreDistinctIntervals()
    {
        var sut = CreateTree((1, 9), (1, 5), (1, 7));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { (1, 5), (1, 7), (1, 9) }, sut.ToList());
    }

    /// <summary>
    /// Verifies that a degenerate interval whose endpoints are equal is accepted.
    /// </summary>
    [TestMethod]
    public void Add_WhenLowEqualsHigh_ShouldStoreDegenerateInterval()
    {
        var sut = new IntervalTree<int>();

        sut.Add(5, 5);

        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.Contains(5, 5));
    }

    /// <summary>
    /// Verifies that adding an interval whose lower endpoint orders after its upper endpoint throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenLowExceedsHigh_ShouldThrowArgumentException()
    {
        var sut = new IntervalTree<int>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(10, 5);
        });

        Assert.AreEqual("low", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding an interval with a <see langword="null" /> endpoint throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add("a", null!);
        });
    }
}

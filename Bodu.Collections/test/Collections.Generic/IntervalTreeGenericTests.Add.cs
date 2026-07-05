// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies that adding overlapping entries stores them all and enumerates them ascending by (low, high).
    /// </summary>
    [TestMethod]
    public void Add_WhenIntervalsOverlap_ShouldStoreAllEntries()
    {
        var sut = CreateTree((10, 20, "b"), (1, 30, "a"), (15, 18, "c"));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(
            new[] { (1, 30, "a"), (10, 20, "b"), (15, 18, "c") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that the same (low, high) interval accepts multiple different values, retained in insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameIntervalDifferentValues_ShouldStoreAllInInsertionOrder()
    {
        var sut = CreateTree((10, 12, "design review"), (10, 12, "1:1"), (10, 12, "retro"));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(
            new[] { (10, 12, "design review"), (10, 12, "1:1"), (10, 12, "retro") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that the same (low, high) interval also accepts equal values as distinct duplicate entries.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameIntervalEqualValues_ShouldStoreDuplicateEntries()
    {
        var sut = CreateTree((10, 12, "shift"), (10, 12, "shift"));

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { (10, 12, "shift"), (10, 12, "shift") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value is storable when <c>TValue</c> is a nullable reference type.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreEntry()
    {
        var sut = new IntervalTree<int, string?>();

        sut.Add(1, 5, null);

        Assert.AreEqual(1, sut.Count);
        CollectionAssert.AreEqual(new[] { (1, 5, (string?)null) }, sut.ToList());
    }

    /// <summary>
    /// Verifies that adding an entry whose lower endpoint orders after its upper endpoint throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenLowExceedsHigh_ShouldThrowArgumentException()
    {
        var sut = new IntervalTree<int, string>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(10, 5, "x");
        });

        Assert.AreEqual("low", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding an entry with a <see langword="null" /> endpoint throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IntervalTree<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!, "b", 1);
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add("a", null!, 1);
        });
    }
}

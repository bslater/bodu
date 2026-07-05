// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.QueryOverlaps.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies that overlap-window queries yield the intersecting entries with their values, ascending by
    /// (low, high), and exclude disjoint entries.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenEntriesVary_ShouldYieldIntersectingEntriesWithValues()
    {
        var sut = CreateTree((1, 5, "before"), (8, 12, "left"), (11, 25, "spanning"), (30, 40, "after"));

        CollectionAssert.AreEqual(
            new[] { (8, 12, "left"), (11, 25, "spanning") }, sut.QueryOverlaps(10, 26).ToList());
    }

    /// <summary>
    /// Verifies that touching windows match under closed semantics and disjoint windows miss.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenWindowTouchesEndpoint_ShouldMatchInclusively()
    {
        var sut = CreateTree((10, 20, "x"));

        Assert.AreEqual(1, sut.QueryOverlaps(5, 10).Count());
        Assert.AreEqual(1, sut.QueryOverlaps(20, 25).Count());
        Assert.AreEqual(0, sut.QueryOverlaps(1, 9).Count());
        Assert.AreEqual(0, sut.QueryOverlaps(21, 30).Count());
    }

    /// <summary>
    /// Verifies that an interval carrying multiple values — including equal ones — yields one entry per stored
    /// value in insertion order.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenIntervalCarriesMultipleValues_ShouldYieldOneEntryPerValue()
    {
        var sut = CreateTree((10, 12, "shift"), (10, 12, "shift"), (10, 12, "cover"));

        CollectionAssert.AreEqual(
            new[] { (10, 12, "shift"), (10, 12, "shift"), (10, 12, "cover") },
            sut.QueryOverlaps(0, 100).ToList());
    }

    /// <summary>
    /// Verifies that the lazy overlap sequence is fail-fast — mutating the tree mid-iteration throws
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenTreeMutatedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 10, "a"), (2, 9, "b"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High, string Value) _ in sut.QueryOverlaps(0, 20))
                sut.Remove(2, 9);
        });
    }

    /// <summary>
    /// Verifies that querying with a reversed window throws <see cref="ArgumentException" /> eagerly and a
    /// <see langword="null" /> edge throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenArgumentsInvalid_ShouldThrow()
    {
        var intTree = CreateTree((1, 5, "x"));
        var stringTree = new IntervalTree<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = intTree.QueryOverlaps(10, 5);
        });
        Assert.AreEqual("low", ex.ParamName);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stringTree.QueryOverlaps(null!, "b");
        });
    }

    /// <summary>
    /// Verifies that the boolean conveniences agree with the window queries, including on an empty tree.
    /// </summary>
    [TestMethod]
    public void QueryOverlaps_WhenComparedWithIntersects_ShouldAgree()
    {
        var sut = CreateTree((10, 20, "x"), (30, 40, "y"));

        Assert.IsTrue(sut.Intersects(15, 35));
        Assert.IsFalse(sut.Intersects(21, 29));
        Assert.IsTrue(sut.IntersectsPoint(10));
        Assert.IsFalse(sut.IntersectsPoint(25));
        Assert.IsFalse(new IntervalTree<int, string>().Intersects(0, 100));
    }
}

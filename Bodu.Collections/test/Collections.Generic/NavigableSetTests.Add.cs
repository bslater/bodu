// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that adding a new item returns <see langword="true" /> and increments <see cref="NavigableSet{T}.Count" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNew_ShouldReturnTrueAndIncrementCount()
    {
        var sut = new NavigableSet<int>();

        bool added = sut.Add(42);

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that adding a comparer-equal duplicate returns <see langword="false" /> and does not change the
    /// count.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldReturnFalseAndNotIncreaseCount()
    {
        var sut = CreateSet(1, 2, 3);

        bool added = sut.Add(2);

        Assert.IsFalse(added);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that a comparer-equal duplicate add preserves the originally stored element.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerEqualDuplicate_ShouldPreserveStoredElement()
    {
        var sut = new NavigableSet<string>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha");

        bool added = sut.Add("ALPHA");

        Assert.IsFalse(added);
        Assert.AreEqual("Alpha", sut.Min);
    }

    /// <summary>
    /// Verifies that items added in descending order enumerate in ascending comparer order.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAddedDescending_ShouldEnumerateAscending()
    {
        var sut = CreateSet(5, 4, 3, 2, 1);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that a monotonically ascending insertion sequence — the classic rebalancing stress for a
    /// self-balancing tree — keeps the set fully queryable.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAddedAscending_ShouldRemainConsistent()
    {
        var sut = new NavigableSet<int>();
        for (int i = 0; i < 256; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(256, sut.Count);
        Assert.AreEqual(0, sut.Min);
        Assert.AreEqual(255, sut.Max);
        for (int rank = 0; rank < 256; rank++)
            Assert.AreEqual(rank, sut.GetAt(rank));
    }

    /// <summary>
    /// Verifies that adding a <see langword="null" /> item throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Add(null!);
        }, "item");
    }
}

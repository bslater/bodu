// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that the indexer rejects out-of-range indices.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void Indexer_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut[index];
        });
    }

    /// <summary>
    /// Verifies that the indexer on an empty set throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetIsEmpty_ShouldThrowExactly()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut[0];
        });
    }

    /// <summary>
    /// Verifies that the indexer returns each stored range in ascending order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetPopulated_ShouldReturnRangesInAscendingOrder()
    {
        RangeSet<int> sut = CreateSet((20, 25), (0, 5), (10, 15));

        Assert.AreEqual(new Range<int>(0, 5), sut[0]);
        Assert.AreEqual(new Range<int>(10, 15), sut[1]);
        Assert.AreEqual(new Range<int>(20, 25), sut[2]);
    }

    /// <summary>
    /// Verifies that the indexer projects a range whose endpoints are ordered by a custom comparer that disagrees
    /// with the default comparer, rather than re-validating with the default order and throwing.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenComparerReversesDefaultOrder_ShouldReturnRange()
    {
        var sut = new RangeSet<int>(new ReverseComparer<int>());
        sut.Add(10, 5); // valid under the reverse comparer; inverted under the default order

        Range<int> range = sut[0];

        Assert.AreEqual(10, range.StartInclusive);
        Assert.AreEqual(5, range.EndExclusive);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    public void Indexer_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
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
    public void Indexer_WhenSetIsEmpty_ShouldThrowArgumentOutOfRangeException()
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

}

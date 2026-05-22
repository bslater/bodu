// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that an exclusive endpoint (immediately past the range) is treated as outside.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyEqualsExclusiveEnd_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut[10];
        });
    }

    /// <summary>
    /// Verifies the value returned through the indexer across boundary and interior positions for a single
    /// range.
    /// </summary>
    [TestMethod]
    [DataRow(0, "A")]
    [DataRow(5, "A")]
    [DataRow(9, "A")]
    public void Indexer_WhenKeyInsideSingleRange_ShouldReturnValue(int key, string expected)
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        Assert.AreEqual(expected, sut[key]);
    }

    /// <summary>
    /// Verifies that the key indexer throws <see cref="KeyNotFoundException" /> when no range contains the key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNotContained_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut[99];
        });
    }
    /// <summary>
    /// Verifies that the key indexer rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut[null!];
        });
    }

    /// <summary>
    /// Verifies the value returned through the indexer when multiple disjoint ranges are present.
    /// </summary>
    [TestMethod]
    [DataRow(0, "A")]
    [DataRow(9, "A")]
    [DataRow(20, "B")]
    [DataRow(29, "B")]
    public void Indexer_WhenMultipleRangesPresent_ShouldReturnEntryForContainingRange(int key, string expected)
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        Assert.AreEqual(expected, sut[key]);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Overlaps.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Overlaps(TKey, TKey)" /> rejects
    /// <see langword="null" /> endpoints.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        RangeDictionary<string, int> sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Overlaps(null!, "z");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Overlaps("a", null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Overlaps(TKey, TKey)" /> rejects degenerate
    /// ranges.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenStartIsNotLessThanEnd_ShouldThrowArgumentException()
    {
        RangeDictionary<int, string> sut = new RangeDictionary<int, string>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Overlaps(5, 5);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Overlaps(TKey, TKey)" /> on an empty dictionary
    /// returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        RangeDictionary<int, string> sut = new RangeDictionary<int, string>();

        Assert.IsFalse(sut.Overlaps(0, 10));
    }

    /// <summary>
    /// Verifies the overlap outcome across representative combinations including touching boundaries.
    /// </summary>
    [TestMethod]
    [DataRow(-5, 0, false)]
    [DataRow(-5, 1, true)]
    [DataRow(0, 5, true)]
    [DataRow(2, 4, true)]
    [DataRow(4, 6, true)]
    [DataRow(5, 10, false)]
    [DataRow(5, 11, true)]
    [DataRow(9, 11, true)]
    [DataRow(15, 20, false)]
    [DataRow(-100, 100, true)]
    public void Overlaps_WhenRangesVary_ShouldReturnExpected(int start, int end, bool expected)
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));

        Assert.AreEqual(expected, sut.Overlaps(start, end));
    }
}

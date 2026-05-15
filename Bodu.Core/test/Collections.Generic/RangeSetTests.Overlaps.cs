// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Overlaps.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Overlaps(T, T)" /> rejects <see langword="null" /> endpoints.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenEndpointIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeSet<string>();

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
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        Assert.AreEqual(expected, sut.Overlaps(start, end));
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Overlaps(T, T)" /> on an empty set returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new RangeSet<int>();

        Assert.IsFalse(sut.Overlaps(0, 10));
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Overlaps(T, T)" /> rejects degenerate ranges.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenStartIsNotLessThanEnd_ShouldThrowArgumentException()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Overlaps(5, 5);
        });
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class RangeTests
{

    /// <summary>
    /// Verifies that a single-step range <c>[n, n+1)</c> contains exactly the value <c>n</c>.
    /// </summary>
    [TestMethod]
    public void Contains_WhenRangeIsSingleStep_ShouldContainStartOnly()
    {
        var sut = new Range<int>(5, 6);

        Assert.IsTrue(sut.Contains(5));
        Assert.IsFalse(sut.Contains(6));
        Assert.IsFalse(sut.Contains(4));
    }

    /// <summary>
    /// Verifies that <see cref="Range{T}.Contains(T)" /> on a string-typed range uses ordinal comparison.
    /// </summary>
    [TestMethod]
    [DataRow("a", true)]
    [DataRow("alpha", true)]
    [DataRow("z", false)]
    [DataRow("", false)]
    [DataRow("zzz", false)]
    public void Contains_WhenStringValuesVary_ShouldReturnExpected(string value, bool expected)
    {
        var sut = new Range<string>("a", "z");

        Assert.AreEqual(expected, sut.Contains(value));
    }

    /// <summary>
    /// Verifies that the exclusive end of the range is not contained.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueEqualsEndExclusive_ShouldReturnFalse()
    {
        var sut = new Range<int>(0, 10);

        Assert.IsFalse(sut.Contains(10));
    }

    /// <summary>
    /// Verifies that the inclusive start of the range is contained.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueEqualsStartInclusive_ShouldReturnTrue()
    {
        var sut = new Range<int>(0, 10);

        Assert.IsTrue(sut.Contains(0));
    }
    /// <summary>
    /// Verifies that <see cref="Range{T}.Contains(T)" /> rejects a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new Range<string>("a", "z");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!);
        });
    }

    /// <summary>
    /// Verifies the membership outcome across an exhaustive set of inputs covering inside, boundary, and
    /// outside positions on both sides of the range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(5, true)]
    [DataRow(9, true)]
    [DataRow(10, false)]
    [DataRow(11, false)]
    [DataRow(int.MinValue, false)]
    [DataRow(int.MaxValue, false)]
    public void Contains_WhenValuesVary_ShouldReturnExpected(int value, bool expected)
    {
        var sut = new Range<int>(0, 10);

        Assert.AreEqual(expected, sut.Contains(value));
    }

}

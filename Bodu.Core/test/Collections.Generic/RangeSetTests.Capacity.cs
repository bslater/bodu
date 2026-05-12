// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{
    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.EnsureCapacity(int)" /> rejects a negative capacity.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.EnsureCapacity(capacity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.EnsureCapacity(int)" /> grows to at least the requested capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenLargerCapacityRequested_ShouldGrow()
    {
        var sut = new RangeSet<int>();

        int reported = sut.EnsureCapacity(128);

        Assert.IsTrue(reported >= 128);
        Assert.IsTrue(sut.Capacity >= 128);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.EnsureCapacity(int)" /> does not shrink when the requested capacity
    /// is below the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenSmallerCapacityRequested_ShouldNotShrink()
    {
        var sut = new RangeSet<int>();
        sut.EnsureCapacity(64);
        int capacityBefore = sut.Capacity;

        int reported = sut.EnsureCapacity(4);

        Assert.AreEqual(capacityBefore, sut.Capacity);
        Assert.AreEqual(capacityBefore, reported);
    }

    /// <summary>
    /// Verifies that the stored contents survive an <see cref="RangeSet{T}.EnsureCapacity(int)" /> growth.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrown_ShouldPreserveContents()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        sut.EnsureCapacity(128);

        AssertContents(sut, (0, 5), (10, 15));
    }

    /// <summary>
    /// Verifies that adding many non-overlapping ranges grows storage automatically while preserving order.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyNonOverlappingRangesAdded_ShouldGrowAndPreserveOrder()
    {
        var sut = new RangeSet<int>();

        for (int i = 0; i < 500; i++)
            sut.Add(i * 10, (i * 10) + 5);

        Assert.AreEqual(500, sut.Count);
        for (int i = 0; i < 500; i++)
            Assert.AreEqual(new Range<int>(i * 10, (i * 10) + 5), sut[i]);
    }
}

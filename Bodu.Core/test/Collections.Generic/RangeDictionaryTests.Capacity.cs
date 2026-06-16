// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Capacity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that adding many non-overlapping ranges grows storage automatically while preserving order.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyNonOverlappingRangesAdded_ShouldGrowAndPreserveOrder()
    {
        var sut = new RangeDictionary<int, string>();

        for (int i = 0; i < 500; i++)
            sut.Add(i * 10, (i * 10) + 5, "v" + i);

        Assert.AreEqual(500, sut.Count);
        for (int i = 0; i < 500; i++)
        {
            ValueRange<int, string> entry = sut.GetEntryAt(i);
            Assert.AreEqual(i * 10, entry.StartInclusive);
            Assert.AreEqual((i * 10) + 5, entry.EndExclusive);
            Assert.AreEqual("v" + i, entry.Value);
        }
    }
    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.EnsureCapacity(int)" /> rejects a negative
    /// capacity.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void EnsureCapacity_WhenCapacityIsNegative_ShouldThrowExactly(int capacity)
    {
        var sut = new RangeDictionary<int, string>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.EnsureCapacity(capacity);
        });
    }

    /// <summary>
    /// Verifies that the stored contents survive an <see cref="RangeDictionary{TKey, TValue}.EnsureCapacity(int)" />
    /// growth.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrown_ShouldPreserveContents()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));

        sut.EnsureCapacity(128);

        AssertContents(sut, (0, 5, "A"), (10, 15, "B"));
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.EnsureCapacity(int)" /> grows to at least the
    /// requested capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenLargerCapacityRequested_ShouldGrow()
    {
        var sut = new RangeDictionary<int, string>();

        int reported = sut.EnsureCapacity(128);

        Assert.IsGreaterThanOrEqualTo(128, reported);
        Assert.IsGreaterThanOrEqualTo(128, sut.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.EnsureCapacity(int)" /> does not shrink when
    /// the requested capacity is below the current capacity.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenSmallerCapacityRequested_ShouldNotShrink()
    {
        var sut = new RangeDictionary<int, string>();
        sut.EnsureCapacity(64);
        int capacityBefore = sut.Capacity;

        int reported = sut.EnsureCapacity(4);

        Assert.AreEqual(capacityBefore, sut.Capacity);
        Assert.AreEqual(capacityBefore, reported);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.MergeCompaction.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Add(T, T)" /> handles a merge that spans every existing range,
    /// leaving the set with a single coalesced range.
    /// </summary>
    [TestMethod]
    public void Add_WhenMergeAbsorbsAllExistingRanges_ShouldYieldSingleCoalescedRange()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15), (20, 25));

        sut.Add(-5, 100);

        AssertContents(sut, (-5, 100));
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Add(T, T)" /> compacts storage and preserves trailing ranges when
    /// the added range merges across multiple interior ranges, exercising the internal <c>RemoveRange</c>
    /// helper's <c>moveCount &gt; 0</c> branch (Array.Copy shifts the trailing entries down).
    /// </summary>
    [TestMethod]
    public void Add_WhenMergeAbsorbsInteriorRangesWithTrailing_ShouldShiftTrailingRangesDown()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15), (20, 25), (40, 50));

        sut.Add(3, 30);

        AssertContents(sut, (0, 30), (40, 50));
    }
    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Add(T, T)" /> compacts storage when the added range merges
    /// across multiple existing ranges at the tail of the set, exercising the internal <c>RemoveRange</c>
    /// helper's <c>moveCount == 0</c> branch (no trailing entries to shift).
    /// </summary>
    [TestMethod]
    public void Add_WhenMergeAbsorbsTailRanges_ShouldCompactStorageWithoutTrailingShift()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15), (20, 25));

        sut.Add(3, 30);

        AssertContents(sut, (0, 30));
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.EnsureCapacity(int)" /> grows from an empty set to the
    /// default-capacity floor when a small but positive capacity is requested, exercising
    /// <c>GrowCapacity</c>'s zero-length-array branch.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenCurrentCapacityIsZero_ShouldReturnAtLeastDefaultCapacity()
    {
        var sut = new RangeSet<int>();
        Assert.AreEqual(0, sut.Capacity);

        var reported = sut.EnsureCapacity(1);

        Assert.IsGreaterThanOrEqualTo(4, reported, $"Expected default capacity floor of 4 or more, got {reported}.");
        Assert.IsGreaterThanOrEqualTo(4, sut.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.EnsureCapacity(int)" /> returns the explicit minimum when the
    /// requested capacity exceeds the doubled current capacity, exercising <c>GrowCapacity</c>'s
    /// <c>capacity &lt; minimum</c> branch.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequiredCapacityExceedsDouble_ShouldReturnRequiredCapacity()
    {
        var sut = new RangeSet<int>();
        sut.EnsureCapacity(4); // initial small capacity

        var reported = sut.EnsureCapacity(1024); // far exceeds 4*2 = 8

        Assert.IsGreaterThanOrEqualTo(1024, reported);
        Assert.IsGreaterThanOrEqualTo(1024, sut.Capacity);
    }

}

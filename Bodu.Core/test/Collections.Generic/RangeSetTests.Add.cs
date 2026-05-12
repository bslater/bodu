// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{
    // --------------------------------------------------------
    // Add(T, T) — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding a range with a <see langword="null" /> start is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenStartIsNull_ShouldThrowArgumentNullException()
    {
        RangeSet<string> sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!, "z");
        });
    }

    /// <summary>
    /// Verifies that adding a range with a <see langword="null" /> end is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenEndIsNull_ShouldThrowArgumentNullException()
    {
        RangeSet<string> sut = new RangeSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add("a", null!);
        });
    }

    /// <summary>
    /// Verifies that adding a range with <c>start &gt;= end</c> is rejected.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(10, 5)]
    public void Add_WhenStartIsNotLessThanEnd_ShouldThrowArgumentException(int start, int end)
    {
        RangeSet<int> sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(start, end);
        });
    }

    // --------------------------------------------------------
    // Add(T, T) — empty set
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding a range to an empty set inserts it as the only entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenSetIsEmpty_ShouldInsertOnly()
    {
        RangeSet<int> sut = new RangeSet<int>();

        sut.Add(5, 10);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (5, 10));
    }

    // --------------------------------------------------------
    // Add(T, T) — non-overlapping ranges keep order
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding non-overlapping ranges in arbitrary order produces a sorted result.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangesAreNonOverlapping_ShouldKeepSortedOrder()
    {
        RangeSet<int> sut = new RangeSet<int>();

        sut.Add(20, 25);
        sut.Add(0, 5);
        sut.Add(10, 15);

        AssertContents(sut, (0, 5), (10, 15), (20, 25));
    }

    /// <summary>
    /// Verifies that adding a range that nests between two existing ranges places it between them.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeNestsBetweenExisting_ShouldInsertInOrder()
    {
        RangeSet<int> sut = CreateSet((0, 5), (20, 25));

        sut.Add(10, 15);

        AssertContents(sut, (0, 5), (10, 15), (20, 25));
    }

    // --------------------------------------------------------
    // Add(T, T) — merge behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an adjacent range (touching at an endpoint) is merged into a contiguous range.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsAdjacentOnLeft_ShouldMergeWithExisting()
    {
        RangeSet<int> sut = CreateSet((5, 10));

        sut.Add(0, 5);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that an adjacent range on the right is merged into a contiguous range.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsAdjacentOnRight_ShouldMergeWithExisting()
    {
        RangeSet<int> sut = CreateSet((0, 5));

        sut.Add(5, 10);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that a range overlapping an existing range from the left expands it.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeOverlapsLeftOfExisting_ShouldExpandLeft()
    {
        RangeSet<int> sut = CreateSet((5, 10));

        sut.Add(0, 7);

        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that a range overlapping an existing range from the right expands it.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeOverlapsRightOfExisting_ShouldExpandRight()
    {
        RangeSet<int> sut = CreateSet((0, 5));

        sut.Add(3, 10);

        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that a range fully contained within an existing range leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsContainedByExisting_ShouldLeaveSetUnchanged()
    {
        RangeSet<int> sut = CreateSet((0, 10));

        sut.Add(2, 5);

        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that adding the exact range that already exists leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeMatchesExisting_ShouldLeaveSetUnchanged()
    {
        RangeSet<int> sut = CreateSet((0, 10));

        sut.Add(0, 10);

        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that adding a range that engulfs an existing range replaces it with the larger range.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeEngulfsExisting_ShouldReplaceWithLargerRange()
    {
        RangeSet<int> sut = CreateSet((3, 5));

        sut.Add(0, 10);

        AssertContents(sut, (0, 10));
    }

    /// <summary>
    /// Verifies that adding a range that spans across multiple existing ranges merges them all into one.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeSpansMultipleExisting_ShouldMergeAllIntoOne()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15), (20, 25));

        sut.Add(3, 22);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 25));
    }

    /// <summary>
    /// Verifies that adding a range that touches multiple existing ranges only at the boundaries merges them.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeBridgesAdjacentExistingRanges_ShouldMergeIntoOne()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        sut.Add(5, 10);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 15));
    }

    // --------------------------------------------------------
    // Add(Range<T>) overload
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="RangeSet{T}.Add(Range{T})" /> overload delegates to the endpoint overload.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeStructProvided_ShouldDelegateToEndpointOverload()
    {
        RangeSet<int> sut = new RangeSet<int>();

        sut.Add(new Range<int>(0, 10));

        AssertContents(sut, (0, 10));
    }

    // --------------------------------------------------------
    // Add — data-driven merge matrix
    // --------------------------------------------------------

    /// <summary>
    /// Verifies the merge outcome across a representative matrix of seed-range and inserted-range
    /// combinations.
    /// </summary>
    [TestMethod]
    [DataRow(10, 20, 0, 5, new[] { 0, 5, 10, 20 })]
    [DataRow(10, 20, 0, 10, new[] { 0, 20 })]
    [DataRow(10, 20, 0, 15, new[] { 0, 20 })]
    [DataRow(10, 20, 0, 20, new[] { 0, 20 })]
    [DataRow(10, 20, 0, 25, new[] { 0, 25 })]
    [DataRow(10, 20, 10, 15, new[] { 10, 20 })]
    [DataRow(10, 20, 15, 20, new[] { 10, 20 })]
    [DataRow(10, 20, 12, 18, new[] { 10, 20 })]
    [DataRow(10, 20, 15, 25, new[] { 10, 25 })]
    [DataRow(10, 20, 20, 25, new[] { 10, 25 })]
    [DataRow(10, 20, 25, 30, new[] { 10, 20, 25, 30 })]
    public void Add_WhenSeedRangeAndInsertVary_ShouldMatchExpected(
        int seedStart, int seedEnd, int insertStart, int insertEnd, int[] expectedFlat)
    {
        RangeSet<int> sut = CreateSet((seedStart, seedEnd));

        sut.Add(insertStart, insertEnd);

        (int Start, int End)[] expected = new (int, int)[expectedFlat.Length / 2];
        for (int i = 0; i < expected.Length; i++)
            expected[i] = (expectedFlat[i * 2], expectedFlat[(i * 2) + 1]);

        AssertContents(sut, expected);
    }
}

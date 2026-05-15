// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.SetOperations.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Except(RangeSet{T})" /> does not mutate either operand.
    /// </summary>
    [TestMethod]
    public void Except_WhenCalled_ShouldNotMutateOperands()
    {
        RangeSet<int> left = CreateSet((0, 20));
        RangeSet<int> right = CreateSet((5, 15));

        _ = left.Except(right);

        AssertContents(left, (0, 20));
        AssertContents(right, (5, 15));
    }

    /// <summary>
    /// Verifies that subtracting an interior range from an existing one splits it into two.
    /// </summary>
    [TestMethod]
    public void Except_WhenInteriorRemoved_ShouldSplitIntoTwo()
    {
        RangeSet<int> left = CreateSet((0, 20));
        RangeSet<int> right = CreateSet((5, 15));

        RangeSet<int> result = left.Except(right);

        AssertContents(result, (0, 5), (15, 20));
    }

    // --------------------------------------------------------
    // Except
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Except(RangeSet{T})" /> rejects a <see langword="null" /> operand.
    /// </summary>
    [TestMethod]
    public void Except_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Except(null!);
        });
    }

    /// <summary>
    /// Verifies that subtracting a range that overlaps an existing one trims it.
    /// </summary>
    [TestMethod]
    public void Except_WhenOverlapTrimsRange_ShouldReturnTrimmedRange()
    {
        RangeSet<int> left = CreateSet((0, 20));
        RangeSet<int> right = CreateSet((10, 30));

        RangeSet<int> result = left.Except(right);

        AssertContents(result, (0, 10));
    }

    /// <summary>
    /// Verifies that subtracting a disjoint set leaves the contents unchanged.
    /// </summary>
    [TestMethod]
    public void Except_WhenSetsAreDisjoint_ShouldReturnContents()
    {
        RangeSet<int> left = CreateSet((0, 5));
        RangeSet<int> right = CreateSet((10, 15));

        RangeSet<int> result = left.Except(right);

        AssertContents(result, (0, 5));
    }

    /// <summary>
    /// Verifies that subtracting an equal set returns an empty result.
    /// </summary>
    [TestMethod]
    public void Except_WhenSetsAreEqual_ShouldReturnEmpty()
    {
        RangeSet<int> left = CreateSet((0, 5), (10, 15));
        RangeSet<int> right = CreateSet((0, 5), (10, 15));

        RangeSet<int> result = left.Except(right);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Intersect(RangeSet{T})" /> does not mutate either operand.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenCalled_ShouldNotMutateOperands()
    {
        RangeSet<int> left = CreateSet((0, 10));
        RangeSet<int> right = CreateSet((5, 15));

        _ = left.Intersect(right);

        AssertContents(left, (0, 10));
        AssertContents(right, (5, 15));
    }

    /// <summary>
    /// Verifies that intersection with an empty set returns an empty result.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOneSideIsEmpty_ShouldReturnEmpty()
    {
        RangeSet<int> populated = CreateSet((0, 10));
        var empty = new RangeSet<int>();

        RangeSet<int> result = populated.Intersect(empty);

        Assert.AreEqual(0, result.Count);
    }

    // --------------------------------------------------------
    // Intersect
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Intersect(RangeSet{T})" /> rejects a <see langword="null" /> operand.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Intersect(null!);
        });
    }

    /// <summary>
    /// Verifies that the intersection of two disjoint sets is empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenSetsAreDisjoint_ShouldReturnEmpty()
    {
        RangeSet<int> left = CreateSet((0, 5));
        RangeSet<int> right = CreateSet((10, 15));

        RangeSet<int> result = left.Intersect(right);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that the intersection of two equal sets returns the same content.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenSetsAreEqual_ShouldReturnContents()
    {
        RangeSet<int> left = CreateSet((0, 5), (10, 15));
        RangeSet<int> right = CreateSet((0, 5), (10, 15));

        RangeSet<int> result = left.Intersect(right);

        AssertContents(result, (0, 5), (10, 15));
    }

    /// <summary>
    /// Verifies that the intersection produces only the overlapping portion of each pair.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenSetsPartiallyOverlap_ShouldReturnOverlapOnly()
    {
        RangeSet<int> left = CreateSet((0, 10), (20, 30));
        RangeSet<int> right = CreateSet((5, 25));

        RangeSet<int> result = left.Intersect(right);

        AssertContents(result, (5, 10), (20, 25));
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Union(RangeSet{T})" /> does not mutate either operand.
    /// </summary>
    [TestMethod]
    public void Union_WhenCalled_ShouldNotMutateOperands()
    {
        RangeSet<int> left = CreateSet((0, 5));
        RangeSet<int> right = CreateSet((10, 15));

        _ = left.Union(right);

        AssertContents(left, (0, 5));
        AssertContents(right, (10, 15));
    }

    /// <summary>
    /// Verifies that the union with an empty set returns the contents of the non-empty operand.
    /// </summary>
    [TestMethod]
    public void Union_WhenOneSideIsEmpty_ShouldReturnContentsOfNonEmpty()
    {
        RangeSet<int> populated = CreateSet((0, 5), (10, 15));
        var empty = new RangeSet<int>();

        RangeSet<int> result = populated.Union(empty);

        AssertContents(result, (0, 5), (10, 15));
    }
    // --------------------------------------------------------
    // Union
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Union(RangeSet{T})" /> rejects a <see langword="null" /> operand.
    /// </summary>
    [TestMethod]
    public void Union_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Union(null!);
        });
    }

    /// <summary>
    /// Verifies that the union of two disjoint sets contains both range groups in sorted order.
    /// </summary>
    [TestMethod]
    public void Union_WhenSetsAreDisjoint_ShouldContainAllRanges()
    {
        RangeSet<int> left = CreateSet((0, 5));
        RangeSet<int> right = CreateSet((10, 15));

        RangeSet<int> result = left.Union(right);

        AssertContents(result, (0, 5), (10, 15));
    }

    /// <summary>
    /// Verifies that the union of overlapping sets merges the overlapping ranges into combined ranges.
    /// </summary>
    [TestMethod]
    public void Union_WhenSetsOverlap_ShouldMergeOverlappingRanges()
    {
        RangeSet<int> left = CreateSet((0, 10));
        RangeSet<int> right = CreateSet((5, 15));

        RangeSet<int> result = left.Union(right);

        AssertContents(result, (0, 15));
    }

}

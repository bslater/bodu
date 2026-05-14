// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvedWindowSetTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies that <see cref="ResolvedWindowSet" /> maintains the minimum number of disjoint intervals describing the union of
/// added ranges and answers coverage queries correctly.
/// </summary>
[TestClass]
public sealed class ResolvedWindowSetTests
{
    /// <summary>
    /// Verifies that a new set is empty.
    /// </summary>
    [TestMethod]
    public void Ranges_WhenNewlyConstructed_ShouldBeEmpty()
    {
        ResolvedWindowSet set = new();
        Assert.AreEqual(0, set.Ranges.Count);
    }

    /// <summary>
    /// Verifies that a single added range is retained without modification.
    /// </summary>
    [TestMethod]
    public void Add_WhenSingleRange_ShouldRetainItVerbatim()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        Assert.AreEqual(1, set.Ranges.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), set.Ranges[0].StartDate);
        Assert.AreEqual(new DateTime(2026, 3, 31), set.Ranges[0].EndDate);
    }

    /// <summary>
    /// Verifies that two disjoint, non-adjacent ranges remain separate.
    /// </summary>
    [TestMethod]
    public void Add_WhenTwoDisjointRanges_ShouldRetainBothInOrder()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));
        set.Add(new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30)));

        Assert.AreEqual(2, set.Ranges.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), set.Ranges[0].StartDate);
        Assert.AreEqual(new DateTime(2026, 6, 1), set.Ranges[1].StartDate);
    }

    /// <summary>
    /// Verifies that two overlapping ranges are merged into a single interval.
    /// </summary>
    [TestMethod]
    public void Add_WhenOverlappingRanges_ShouldMergeIntoSingleInterval()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));
        set.Add(new DateRange(new DateTime(2026, 3, 15), new DateTime(2026, 5, 31)));

        Assert.AreEqual(1, set.Ranges.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), set.Ranges[0].StartDate);
        Assert.AreEqual(new DateTime(2026, 5, 31), set.Ranges[0].EndDate);
    }

    /// <summary>
    /// Verifies that two ranges meeting on consecutive days are merged into a single interval.
    /// </summary>
    [TestMethod]
    public void Add_WhenAdjacentRanges_ShouldMergeIntoSingleInterval()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));
        set.Add(new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)));

        Assert.AreEqual(1, set.Ranges.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), set.Ranges[0].StartDate);
        Assert.AreEqual(new DateTime(2026, 2, 28), set.Ranges[0].EndDate);
    }

    /// <summary>
    /// Verifies that adding a range that bridges two existing disjoint intervals collapses them into one.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeBridgesTwoDisjointIntervals_ShouldCollapseIntoOne()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));
        set.Add(new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30)));
        set.Add(new DateRange(new DateTime(2026, 1, 15), new DateTime(2026, 6, 15)));

        Assert.AreEqual(1, set.Ranges.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), set.Ranges[0].StartDate);
        Assert.AreEqual(new DateTime(2026, 6, 30), set.Ranges[0].EndDate);
    }

    /// <summary>
    /// Verifies that an inverted range is silently ignored.
    /// </summary>
    [TestMethod]
    public void Add_WhenInvertedRange_ShouldBeIgnored()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 6, 30), new DateTime(2026, 6, 1)));

        Assert.AreEqual(0, set.Ranges.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Covers" /> returns true when the probe is fully inside a tracked interval.
    /// </summary>
    [TestMethod]
    public void Covers_WhenProbeIsInsideTrackedInterval_ShouldReturnTrue()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));

        Assert.IsTrue(set.Covers(new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30))));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Covers" /> returns false when the probe spans a gap between two tracked
    /// intervals — coverage is per-interval, not per-union.
    /// </summary>
    [TestMethod]
    public void Covers_WhenProbeSpansGapBetweenTwoIntervals_ShouldReturnFalse()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));
        set.Add(new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30)));

        Assert.IsFalse(set.Covers(new DateRange(new DateTime(2026, 3, 15), new DateTime(2026, 6, 15))));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Clear" /> empties the set.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllRanges()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        set.Clear();

        Assert.AreEqual(0, set.Ranges.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        ResolvedWindowSet set = new();

        Assert.IsFalse(set.Contains(new DateTime(2026, 1, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="true" /> when the date
    /// lies inside a tracked range.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsInsideTrackedRange_ShouldReturnTrue()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        Assert.IsTrue(set.Contains(new DateTime(2026, 2, 15)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="true" /> when the date
    /// equals the inclusive start boundary.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsOnStartBoundary_ShouldReturnTrue()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        Assert.IsTrue(set.Contains(new DateTime(2026, 1, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="true" /> when the date
    /// equals the inclusive end boundary.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsOnEndBoundary_ShouldReturnTrue()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        Assert.IsTrue(set.Contains(new DateTime(2026, 3, 31)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="false" /> when the date
    /// is earlier than every tracked range.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsBeforeEveryTrackedRange_ShouldReturnFalse()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));

        Assert.IsFalse(set.Contains(new DateTime(2025, 12, 31)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> returns <see langword="false" /> when the date
    /// falls in the gap between two disjoint tracked ranges.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsInGapBetweenTwoTrackedRanges_ShouldReturnFalse()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)));
        set.Add(new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30)));

        Assert.IsFalse(set.Contains(new DateTime(2026, 5, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="ResolvedWindowSet.Contains(DateTime)" /> ignores the time component when comparing days.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateHasTimeComponent_ShouldIgnoreTimeOfDay()
    {
        ResolvedWindowSet set = new();
        set.Add(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1)));

        Assert.IsTrue(set.Contains(new DateTime(2026, 1, 1, 23, 59, 59)));
    }
}

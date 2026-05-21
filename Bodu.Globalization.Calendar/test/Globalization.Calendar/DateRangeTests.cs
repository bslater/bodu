// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the day-resolution semantics of <see cref="DateRange" /> across the inclusive start/end contract,
/// containment, intersection, and the friendly <see cref="DateRange.ToString" /> rendering.
/// </summary>
[TestClass]
public sealed class DateRangeTests
{
    /// <summary>
    /// Verifies that <see cref="DateRange.IsValid" /> returns <see langword="true" /> when the start equals the end.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStartEqualsEnd_ShouldReturnTrue()
    {
        DateRange range = new(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        Assert.IsTrue(range.IsValid);
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.IsValid" /> returns <see langword="false" /> when the start is after the end
    /// (inverted range).
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStartIsAfterEnd_ShouldReturnFalse()
    {
        DateRange range = new(new DateTime(2026, 12, 31), new DateTime(2026, 1, 1));

        Assert.IsFalse(range.IsValid);
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.DayCount" /> returns 1 for a single-day range.
    /// </summary>
    [TestMethod]
    public void DayCount_WhenStartEqualsEnd_ShouldReturnOne()
    {
        DateRange range = new(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        Assert.AreEqual(1, range.DayCount);
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.DayCount" /> returns the inclusive day count for a multi-day range.
    /// </summary>
    [TestMethod]
    public void DayCount_WhenRangeSpansMultipleDays_ShouldReturnInclusiveCount()
    {
        DateRange range = new(new DateTime(2026, 1, 1), new DateTime(2026, 1, 5));

        Assert.AreEqual(5, range.DayCount);
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.DayCount" /> returns zero for an inverted range.
    /// </summary>
    [TestMethod]
    public void DayCount_WhenRangeIsInverted_ShouldReturnZero()
    {
        DateRange range = new(new DateTime(2026, 6, 30), new DateTime(2026, 6, 1));

        Assert.AreEqual(0, range.DayCount);
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateTime)" /> returns <see langword="false" /> for a date before the
    /// inclusive start.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsBeforeStart_ShouldReturnFalse()
    {
        DateRange range = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        Assert.IsFalse(range.Contains(new DateTime(2026, 5, 31)));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateTime)" /> returns <see langword="true" /> for a date on the
    /// inclusive start.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsOnStart_ShouldReturnTrue()
    {
        DateRange range = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        Assert.IsTrue(range.Contains(new DateTime(2026, 6, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateTime)" /> returns <see langword="true" /> for a date on the
    /// inclusive end.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsOnEnd_ShouldReturnTrue()
    {
        DateRange range = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        Assert.IsTrue(range.Contains(new DateTime(2026, 6, 30)));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateTime)" /> returns <see langword="false" /> for a date after the
    /// inclusive end.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateIsAfterEnd_ShouldReturnFalse()
    {
        DateRange range = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        Assert.IsFalse(range.Contains(new DateTime(2026, 7, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateTime)" /> ignores the time-of-day component.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateHasTimeComponent_ShouldIgnoreTimeOfDay()
    {
        DateRange range = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 1));

        Assert.IsTrue(range.Contains(new DateTime(2026, 6, 1, 23, 59, 59)));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateRange)" /> returns <see langword="true" /> when the candidate
    /// range is fully inside this range.
    /// </summary>
    [TestMethod]
    public void ContainsRange_WhenCandidateRangeIsFullyInside_ShouldReturnTrue()
    {
        DateRange outer = new(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        DateRange inner = new(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        Assert.IsTrue(outer.Contains(inner));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Contains(DateRange)" /> returns <see langword="false" /> when the candidate
    /// range extends beyond this range.
    /// </summary>
    [TestMethod]
    public void ContainsRange_WhenCandidateRangeExtendsBeyond_ShouldReturnFalse()
    {
        DateRange outer = new(new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));
        DateRange overlap = new(new DateTime(2026, 6, 15), new DateTime(2026, 7, 15));

        Assert.IsFalse(outer.Contains(overlap));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Intersects" /> returns <see langword="true" /> when the ranges overlap.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenRangesOverlap_ShouldReturnTrue()
    {
        DateRange a = new(new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));
        DateRange b = new(new DateTime(2026, 6, 15), new DateTime(2026, 12, 31));

        Assert.IsTrue(a.Intersects(b));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Intersects" /> returns <see langword="true" /> when the ranges touch on a
    /// shared boundary day.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenRangesTouchAtBoundary_ShouldReturnTrue()
    {
        DateRange a = new(new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));
        DateRange b = new(new DateTime(2026, 6, 30), new DateTime(2026, 12, 31));

        Assert.IsTrue(a.Intersects(b));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Intersects" /> returns <see langword="false" /> when the ranges are disjoint.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenRangesAreDisjoint_ShouldReturnFalse()
    {
        DateRange a = new(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
        DateRange b = new(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30));

        Assert.IsFalse(a.Intersects(b));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.Intersects" /> returns <see langword="false" /> when either range is inverted.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenEitherRangeIsInverted_ShouldReturnFalse()
    {
        DateRange valid = new(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        DateRange inverted = new(new DateTime(2026, 12, 31), new DateTime(2026, 1, 1));

        Assert.IsFalse(valid.Intersects(inverted));
        Assert.IsFalse(inverted.Intersects(valid));
    }

    /// <summary>
    /// Verifies that <see cref="DateRange.ToString" /> renders both bounds in <c>yyyy-MM-dd</c> form.
    /// </summary>
    [TestMethod]
    public void ToString_ShouldIncludeStartAndEndDates()
    {
        DateRange range = new(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var text = range.ToString();

        Assert.IsTrue(text.Contains("2026-01-01", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("2026-12-31", StringComparison.Ordinal));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the range-vs-range containment and overlap operations on <see cref="DateRange" />.
/// </summary>
[TestClass]
public sealed class DateRangeTests
{
    /// <summary>
    /// Builds a January 2025 range from a start and end day.
    /// </summary>
    /// <param name="startDay">The start day of month.</param>
    /// <param name="endDay">The end day of month.</param>
    /// <returns>The range.</returns>
    private static DateRange Range(int startDay, int endDay) =>
        new(new DateOnly(2025, 1, startDay), new DateOnly(2025, 1, endDay));

    /// <summary>
    /// Verifies that a range contains a range fully within its bounds, including an identical range.
    /// </summary>
    /// <param name="outerStart">The outer range start day.</param>
    /// <param name="outerEnd">The outer range end day.</param>
    /// <param name="innerStart">The inner range start day.</param>
    /// <param name="innerEnd">The inner range end day.</param>
    [TestMethod]
    [DataRow(1, 10, 3, 7)]    // strictly within
    [DataRow(1, 10, 1, 10)]   // identical range
    public void Contains_WhenInnerWithinOuter_ShouldReturnTrue(int outerStart, int outerEnd, int innerStart, int innerEnd)
    {
        Assert.IsTrue(Range(outerStart, outerEnd).Contains(Range(innerStart, innerEnd)));
    }

    /// <summary>
    /// Verifies that a range does not contain a range that extends past either bound.
    /// </summary>
    /// <param name="outerStart">The outer range start day.</param>
    /// <param name="outerEnd">The outer range end day.</param>
    /// <param name="innerStart">The inner range start day.</param>
    /// <param name="innerEnd">The inner range end day.</param>
    [TestMethod]
    [DataRow(3, 7, 1, 10)]    // inner extends past both bounds
    [DataRow(1, 5, 4, 8)]     // inner extends past the upper bound
    public void Contains_WhenInnerExtendsBeyond_ShouldReturnFalse(int outerStart, int outerEnd, int innerStart, int innerEnd)
    {
        Assert.IsFalse(Range(outerStart, outerEnd).Contains(Range(innerStart, innerEnd)));
    }

    /// <summary>
    /// Verifies that containment is false when either range is not well-formed.
    /// </summary>
    /// <param name="outerStart">The outer range start day.</param>
    /// <param name="outerEnd">The outer range end day.</param>
    /// <param name="innerStart">The inner range start day.</param>
    /// <param name="innerEnd">The inner range end day.</param>
    [TestMethod]
    [DataRow(10, 1, 3, 7)]    // outer range reversed
    [DataRow(1, 10, 7, 3)]    // inner range reversed
    public void Contains_WhenEitherRangeInvalid_ShouldReturnFalse(int outerStart, int outerEnd, int innerStart, int innerEnd)
    {
        Assert.IsFalse(Range(outerStart, outerEnd).Contains(Range(innerStart, innerEnd)));
    }

    /// <summary>
    /// Verifies that overlapping ranges intersect, regardless of order.
    /// </summary>
    /// <param name="firstStart">The first range start day.</param>
    /// <param name="firstEnd">The first range end day.</param>
    /// <param name="secondStart">The second range start day.</param>
    /// <param name="secondEnd">The second range end day.</param>
    [TestMethod]
    [DataRow(1, 5, 4, 8)]     // overlap
    [DataRow(4, 8, 1, 5)]     // overlap with operands swapped
    public void Intersects_WhenRangesOverlap_ShouldReturnTrue(int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Assert.IsTrue(Range(firstStart, firstEnd).Intersects(Range(secondStart, secondEnd)));
    }

    /// <summary>
    /// Verifies that ranges sharing a single endpoint day intersect.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenSharingSingleEndpoint_ShouldReturnTrue()
    {
        Assert.IsTrue(Range(1, 5).Intersects(Range(5, 9)));
    }

    /// <summary>
    /// Verifies that disjoint ranges do not intersect.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenRangesDisjoint_ShouldReturnFalse()
    {
        Assert.IsFalse(Range(1, 4).Intersects(Range(5, 9)));
    }

    /// <summary>
    /// Verifies that intersection is false when either range is not well-formed.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenEitherRangeInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(Range(5, 1).Intersects(Range(1, 9)));
    }
}

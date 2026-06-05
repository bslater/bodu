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
    [TestMethod]
    public void Contains_WhenInnerWithinOuter_ShouldReturnTrue()
    {
        Assert.IsTrue(Range(1, 10).Contains(Range(3, 7)));
        Assert.IsTrue(Range(1, 10).Contains(Range(1, 10)));
    }

    /// <summary>
    /// Verifies that a range does not contain a range that extends past either bound.
    /// </summary>
    [TestMethod]
    public void Contains_WhenInnerExtendsBeyond_ShouldReturnFalse()
    {
        Assert.IsFalse(Range(3, 7).Contains(Range(1, 10)));
        Assert.IsFalse(Range(1, 5).Contains(Range(4, 8)));
    }

    /// <summary>
    /// Verifies that containment is false when either range is not well-formed.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEitherRangeInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(Range(10, 1).Contains(Range(3, 7)));
        Assert.IsFalse(Range(1, 10).Contains(Range(7, 3)));
    }

    /// <summary>
    /// Verifies that overlapping ranges intersect, regardless of order.
    /// </summary>
    [TestMethod]
    public void Intersects_WhenRangesOverlap_ShouldReturnTrue()
    {
        Assert.IsTrue(Range(1, 5).Intersects(Range(4, 8)));
        Assert.IsTrue(Range(4, 8).Intersects(Range(1, 5)));
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

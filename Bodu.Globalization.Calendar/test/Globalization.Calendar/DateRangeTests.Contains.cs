// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DateRangeTests
{
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
    /// Verifies that <see cref="DateRange.Contains(DateOnly)" /> includes the endpoints and excludes dates outside the
    /// range.
    /// </summary>
    [TestMethod]
    public void Contains_ShouldIncludeEndpointsAndExcludeOutside()
    {
        DateRange range = Range(5, 10);

        Assert.IsTrue(range.Contains(new DateOnly(2025, 1, 5)));
        Assert.IsTrue(range.Contains(new DateOnly(2025, 1, 10)));
        Assert.IsFalse(range.Contains(new DateOnly(2025, 1, 4)));
        Assert.IsFalse(range.Contains(new DateOnly(2025, 1, 11)));
    }
}

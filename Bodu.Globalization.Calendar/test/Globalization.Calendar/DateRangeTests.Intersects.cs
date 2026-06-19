// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.Intersects.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DateRangeTests
{
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

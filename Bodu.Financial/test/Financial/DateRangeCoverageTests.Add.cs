// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeCoverageTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class DateRangeCoverageTests
{
    /// <summary>
    /// Verifies that recording a range whose start is later than its end throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        DateRangeCoverage coverage = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => coverage.Add(On(5), On(3)),
            "start");
    }

    /// <summary>
    /// Verifies that recording a range into an empty coverage produces a single interval spanning it.
    /// </summary>
    [TestMethod]
    public void Add_WhenEmpty_ShouldCreateSingleInterval()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(2), On(6));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(1, intervals);
        Assert.AreEqual((On(2), On(6)), intervals[0]);
    }

    /// <summary>
    /// Verifies that a range overlapping an existing interval is merged into a single widened interval.
    /// </summary>
    [TestMethod]
    public void Add_WhenOverlappingExisting_ShouldMergeIntoOneInterval()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(5));
        coverage.Add(On(3), On(8));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(1, intervals);
        Assert.AreEqual((On(1), On(8)), intervals[0]);
    }

    /// <summary>
    /// Verifies that a range beginning on the day after an existing interval is treated as adjacent and merged.
    /// </summary>
    [TestMethod]
    public void Add_WhenAdjacentToExisting_ShouldMergeIntoOneInterval()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(3));
        coverage.Add(On(4), On(6));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(1, intervals);
        Assert.AreEqual((On(1), On(6)), intervals[0]);
    }

    /// <summary>
    /// Verifies that ranges separated by at least one unobserved day remain distinct intervals.
    /// </summary>
    [TestMethod]
    public void Add_WhenSeparatedByGap_ShouldKeepIntervalsSeparate()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(3));
        coverage.Add(On(5), On(7));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(2, intervals);
        Assert.AreEqual((On(1), On(3)), intervals[0]);
        Assert.AreEqual((On(5), On(7)), intervals[1]);
    }

    /// <summary>
    /// Verifies that a range bridging the gap between two existing intervals absorbs both into one interval.
    /// </summary>
    [TestMethod]
    public void Add_WhenSpanningGapBetweenIntervals_ShouldMergeAllIntoOne()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(3));
        coverage.Add(On(7), On(9));
        coverage.Add(On(2), On(8));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(1, intervals);
        Assert.AreEqual((On(1), On(9)), intervals[0]);
    }

    /// <summary>
    /// Verifies that a range wholly contained within an existing interval leaves the coverage unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenContainedWithinExisting_ShouldLeaveCoverageUnchanged()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(10));
        coverage.Add(On(3), On(5));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;
        Assert.HasCount(1, intervals);
        Assert.AreEqual((On(1), On(10)), intervals[0]);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behavior of <see cref="DateRangeCoverage" />, the gap-respecting set of observed date ranges.
/// </summary>
[TestClass]
public partial class DateRangeCoverageTests
{
    /// <summary>
    /// Verifies that a range recorded through <see cref="DateRangeCoverage.Add" /> is subsequently reported as covered.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Coverage_WhenRangeAddedThenQueried_ShouldReportCovered()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(3), On(10));

        Assert.IsTrue(coverage.Contains(On(4), On(9)));
    }

    /// <summary>
    /// Verifies that a freshly constructed coverage reports itself empty.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenNothingAdded_ShouldReturnTrue()
    {
        DateRangeCoverage coverage = new();

        Assert.IsTrue(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that recording a range clears the empty state.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenRangeAdded_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(1), On(2));

        Assert.IsFalse(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that ranges separated by a gap are exposed as ordered, disjoint intervals.
    /// </summary>
    [TestMethod]
    public void Intervals_WhenRangesAddedAcrossGap_ShouldReturnOrderedDisjointIntervals()
    {
        DateRangeCoverage coverage = new();

        coverage.Add(On(5), On(7));
        coverage.Add(On(1), On(3));

        IReadOnlyList<(DateOnly Start, DateOnly End)> intervals = coverage.Intervals;

        Assert.AreEqual(2, intervals.Count);
        Assert.AreEqual((On(1), On(3)), intervals[0]);
        Assert.AreEqual((On(5), On(7)), intervals[1]);
    }

    /// <summary>
    /// Returns a date in January 2024 with the supplied day component, for compact construction of test ranges.
    /// </summary>
    /// <param name="day">The one-based day of the month.</param>
    /// <returns>The date 2024-01-<paramref name="day" />.</returns>
    private static DateOnly On(int day) => new(2024, 1, day);
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeCoverageTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class DateRangeCoverageTests
{
    /// <summary>
    /// Verifies that an empty coverage reports no window as covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEmpty_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();

        Assert.IsFalse(coverage.Contains(On(1), On(2)));
    }

    /// <summary>
    /// Verifies that a window strictly inside a single recorded interval is reported as covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenWindowWithinSingleInterval_ShouldReturnTrue()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(10));

        Assert.IsTrue(coverage.Contains(On(3), On(7)));
    }

    /// <summary>
    /// Verifies that a window exactly equal to a recorded interval is reported as covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenWindowEqualsInterval_ShouldReturnTrue()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(4), On(8));

        Assert.IsTrue(coverage.Contains(On(4), On(8)));
    }

    /// <summary>
    /// Verifies that a window straddling the gap between two recorded intervals is reported as not covered, even though
    /// both its endpoints fall inside recorded intervals.
    /// </summary>
    [TestMethod]
    public void Contains_WhenWindowStraddlesGap_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(3));
        coverage.Add(On(5), On(7));

        Assert.IsFalse(coverage.Contains(On(2), On(6)));
    }

    /// <summary>
    /// Verifies that a window extending past the end of the only recorded interval is reported as not covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenWindowExtendsBeyondInterval_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(5));

        Assert.IsFalse(coverage.Contains(On(3), On(8)));
    }

    /// <summary>
    /// Verifies that a single date inside a recorded interval is reported as covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateCovered_ShouldReturnTrue()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(5));

        Assert.IsTrue(coverage.Contains(On(3)));
    }

    /// <summary>
    /// Verifies that a single date falling in a gap between recorded intervals is reported as not covered.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDateInGap_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(3));
        coverage.Add(On(5), On(7));

        Assert.IsFalse(coverage.Contains(On(4)));
    }

    /// <summary>
    /// Verifies that querying a window whose start is later than its end throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(10));

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => coverage.Contains(On(8), On(2)),
            "start");
    }
}

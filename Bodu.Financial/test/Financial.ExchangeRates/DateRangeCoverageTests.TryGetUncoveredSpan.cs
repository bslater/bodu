// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeCoverageTests.TryGetUncoveredSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class DateRangeCoverageTests
{
    /// <summary>
    /// Verifies that an empty coverage reports the whole window as its uncovered span.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenEmpty_ShouldReturnTheWholeWindow()
    {
        DateRangeCoverage coverage = new();

        bool found = coverage.TryGetUncoveredSpan(On(3), On(9), out DateOnly start, out DateOnly end);

        Assert.IsTrue(found);
        Assert.AreEqual(On(3), start);
        Assert.AreEqual(On(9), end);
    }

    /// <summary>
    /// Verifies that a fully covered window has no uncovered span.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenWindowCovered_ShouldReturnFalse()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(20));

        bool found = coverage.TryGetUncoveredSpan(On(3), On(9), out DateOnly start, out DateOnly end);

        Assert.IsFalse(found);
        Assert.AreEqual(default, start);
        Assert.AreEqual(default, end);
    }

    /// <summary>
    /// Verifies that a window whose start is covered reports the days after the covered part.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenStartCovered_ShouldReturnTheDaysAfter()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(10));

        _ = coverage.TryGetUncoveredSpan(On(1), On(15), out DateOnly start, out DateOnly end);

        Assert.AreEqual(On(11), start);
        Assert.AreEqual(On(15), end);
    }

    /// <summary>
    /// Verifies that a window whose end is covered reports the days before the covered part.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenEndCovered_ShouldReturnTheDaysBefore()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(10), On(20));

        _ = coverage.TryGetUncoveredSpan(On(1), On(20), out DateOnly start, out DateOnly end);

        Assert.AreEqual(On(1), start);
        Assert.AreEqual(On(9), end);
    }

    /// <summary>
    /// Verifies that a window covered at both ends reports the gap between.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenBothEndsCovered_ShouldReturnTheGap()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(5));
        coverage.Add(On(10), On(20));

        _ = coverage.TryGetUncoveredSpan(On(1), On(20), out DateOnly start, out DateOnly end);

        Assert.AreEqual(On(6), start);
        Assert.AreEqual(On(9), end);
    }

    /// <summary>
    /// Verifies that a window with several gaps reports one span from the first uncovered day to the last.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenSeveralGaps_ShouldSpanFromFirstToLastUncoveredDay()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(1), On(3));
        coverage.Add(On(6), On(7));
        coverage.Add(On(10), On(12));

        _ = coverage.TryGetUncoveredSpan(On(1), On(12), out DateOnly start, out DateOnly end);

        Assert.AreEqual(On(4), start);
        Assert.AreEqual(On(9), end);
    }

    /// <summary>
    /// Verifies that a window covered only in its middle reports the whole window.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenOnlyMiddleCovered_ShouldReturnTheWholeWindow()
    {
        DateRangeCoverage coverage = new();
        coverage.Add(On(5), On(10));

        _ = coverage.TryGetUncoveredSpan(On(1), On(15), out DateOnly start, out DateOnly end);

        Assert.AreEqual(On(1), start);
        Assert.AreEqual(On(15), end);
    }

    /// <summary>
    /// Verifies that a window whose start is later than its end throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        DateRangeCoverage coverage = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => coverage.TryGetUncoveredSpan(On(8), On(2), out _, out _),
            "start");
    }
}

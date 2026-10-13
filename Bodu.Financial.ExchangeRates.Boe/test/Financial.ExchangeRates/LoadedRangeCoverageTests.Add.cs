// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedRangeCoverageTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class LoadedRangeCoverageTests
{
    /// <summary>
    /// Verifies that a range downloaded after it closed is held for good, long after the refresh interval.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsClosed_ShouldHoldItForGood()
    {
        LoadedRangeCoverage coverage = new();

        coverage.Add(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 31), new DateOnly(2022, 12, 30), s_fetchedAt, s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 31), s_fetchedAt.AddYears(1)));
    }

    /// <summary>
    /// Verifies that the days of a range from the day it was downloaded on, with no rate returned for them, are held only
    /// until the refresh interval has passed, while the days before are held for good.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeReachesToday_ShouldHoldTodayOnlyUntilTheIntervalPasses()
    {
        LoadedRangeCoverage coverage = new();

        coverage.Add(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(11)));
        Assert.IsFalse(coverage.Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(12)));
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 3), s_fetchedAt.AddDays(30)));
    }

    /// <summary>
    /// Verifies that a range that already returned a rate for the day it was downloaded on is held for good through that
    /// day.
    /// </summary>
    [TestMethod]
    public void Add_WhenTodayWasReturned_ShouldHoldTodayForGood()
    {
        LoadedRangeCoverage coverage = new();

        coverage.Add(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 4), s_fetchedAt, s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddDays(30)));
    }

    /// <summary>
    /// Verifies that a range lying wholly after the day it was downloaded on is held only until the refresh interval has
    /// passed.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsInTheFuture_ShouldHoldItOnlyUntilTheIntervalPasses()
    {
        LoadedRangeCoverage coverage = new();

        coverage.Add(new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 3), null, s_fetchedAt, s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 3), s_fetchedAt.AddHours(1)));
        Assert.IsFalse(coverage.Contains(new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 1), s_fetchedAt.AddHours(13)));
    }

    /// <summary>
    /// Verifies that a range whose start is later than its end throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenStartIsAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        LoadedRangeCoverage coverage = new();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            coverage.Add(new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 4), null, s_fetchedAt, s_refresh);
        });
    }
}

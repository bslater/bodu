// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedRangeCoverageTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class LoadedRangeCoverageTests
{
    /// <summary>
    /// Verifies that nothing is held before anything is recorded.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(new LoadedRangeCoverage().Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 2), s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a range held partly for good and partly provisionally, by different downloads, is held while the
    /// provisional part has not expired.
    /// </summary>
    [TestMethod]
    public void Contains_WhenFinalAndProvisionalDaysAdjoin_ShouldJoinThem()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2022, 12, 20), new DateOnly(2022, 12, 31), null, s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 6), null, s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3), null, s_fetchedAt, s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2022, 12, 20), new DateOnly(2023, 1, 6), s_fetchedAt.AddHours(1)));
        Assert.IsFalse(coverage.Contains(new DateOnly(2022, 12, 20), new DateOnly(2023, 1, 6), s_fetchedAt.AddHours(13)));
    }

    /// <summary>
    /// Verifies that a gap between recorded ranges is not held.
    /// </summary>
    [TestMethod]
    public void Contains_WhenRangeStraddlesGap_ShouldReturnFalse()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 10), null, s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2022, 12, 12), new DateOnly(2022, 12, 20), null, s_fetchedAt, s_refresh);

        Assert.IsFalse(coverage.Contains(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 20), s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a later download that returns today's rate holds today for good, after an earlier download's
    /// provisional record of it has expired.
    /// </summary>
    [TestMethod]
    public void Contains_WhenLaterDownloadReturnsToday_ShouldHoldTodayForGood()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(13), s_refresh);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddDays(30)));
    }

    /// <summary>
    /// Verifies that a range whose start is later than its end throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStartIsAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        LoadedRangeCoverage coverage = new();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = coverage.Contains(new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 4), s_fetchedAt);
        });
    }
}

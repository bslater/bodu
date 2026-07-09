// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileRateCacheTests.Partitioning.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that a partitioned <see cref="RateCacheFileLayout" /> splits a pair's rows and coverage across one
/// file per calendar period, isolates each pair in its own folder, and re-merges losslessly on read.
/// </summary>
public sealed partial class TomlFileRateCacheTests
{
    /// <summary>The directory holding the test pair's partition files under a partitioned layout.</summary>
    private string PairDirectory => Path.Combine(_directory, Provider, "AUDUSD");

    /// <summary>
    /// Verifies that a monthly layout writes one file per calendar month under the pair's folder and writes no
    /// single-file-style pair file.
    /// </summary>
    [TestMethod]
    public void Store_WhenLayoutIsMonthly_ShouldWriteOneFilePerMonth()
    {
        TomlFileRateCache cache = CreateCache(RateCacheFileLayout.Monthly);
        DateTimeOffset now = new(2023, 3, 1, 0, 0, 0, TimeSpan.Zero);

        cache.Store(
            Pair,
            new[]
            {
                new CachedRate(new DateOnly(2023, 1, 3), 0.50m, now),
                new CachedRate(new DateOnly(2023, 2, 6), 0.51m, now),
            },
            Duration,
            now);

        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-01.toml")), "the January partition file is written");
        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-02.toml")), "the February partition file is written");
        Assert.IsFalse(File.Exists(Path.Combine(_directory, Provider, "AUDUSD.toml")), "no single-file pair file is written");
    }

    /// <summary>
    /// Verifies that a monthly partition file records only the rows for its own month, confirming rows are routed by
    /// their observation date.
    /// </summary>
    [TestMethod]
    public void Store_WhenLayoutIsMonthly_ShouldRouteRowsToTheirMonthFile()
    {
        TomlFileRateCache cache = CreateCache(RateCacheFileLayout.Monthly);
        DateTimeOffset now = new(2023, 3, 1, 0, 0, 0, TimeSpan.Zero);

        cache.Store(
            Pair,
            new[]
            {
                new CachedRate(new DateOnly(2023, 1, 3), 0.50m, now),
                new CachedRate(new DateOnly(2023, 2, 6), 0.51m, now),
            },
            Duration,
            now);

        string january = File.ReadAllText(Path.Combine(PairDirectory, "2023-01.toml"));

        StringAssert.Contains(january, "Date = 2023-01-03", StringComparison.Ordinal);
        Assert.IsFalse(january.Contains("2023-02-06", StringComparison.Ordinal), "February rows do not leak into the January file");
    }

    /// <summary>
    /// Verifies that a reopened monthly cache reads and merges every partition file, serving the pair's whole history.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetRates_WhenLayoutIsMonthlyAndReopened_ShouldMergeAcrossPartitions()
    {
        DateTimeOffset now = new(2023, 3, 1, 0, 0, 0, TimeSpan.Zero);
        CreateCache(RateCacheFileLayout.Monthly).Store(
            Pair,
            new[]
            {
                new CachedRate(new DateOnly(2023, 1, 3), 0.50m, now),
                new CachedRate(new DateOnly(2023, 2, 6), 0.51m, now),
            },
            Duration,
            now);

        TomlFileRateCache reopened = CreateCache(RateCacheFileLayout.Monthly);
        IReadOnlyList<CachedRate> read = reopened.GetRates(Pair, Duration, now);

        Assert.HasCount(2, read);
        CollectionAssert.AreEquivalent(new[] { 0.50m, 0.51m }, read.Select(r => r.Rate).ToArray());
    }

    /// <summary>
    /// Verifies that a coverage window spanning a partition boundary is split into one sub-window per month on write and
    /// re-merged into the original inclusive range on read, confirming the split is lossless.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenWindowSpansPartitions_ShouldSplitAndRoundTrip()
    {
        DateTimeOffset now = new(2023, 3, 1, 0, 0, 0, TimeSpan.Zero);
        TomlFileRateCache cache = CreateCache(RateCacheFileLayout.Monthly);

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 15), new DateOnly(2023, 2, 15), Duration, now);

        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-01.toml")), "the window's January segment is stored");
        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-02.toml")), "the window's February segment is stored");

        TomlFileRateCache reopened = CreateCache(RateCacheFileLayout.Monthly);
        Assert.IsTrue(
            reopened.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 15), new DateOnly(2023, 2, 15)),
            "the split window re-merges into its original inclusive range on read");
    }

    /// <summary>
    /// Verifies that re-storing rows at an instant past the cached rows' freshness prunes a now-stale partition's rows
    /// and deletes its file, mirroring how a single-file cache deletes an emptied file.
    /// </summary>
    [TestMethod]
    public void Store_WhenPartitionPrunedToEmpty_ShouldDeleteItsFile()
    {
        var januaryCachedAt = new DateTimeOffset(2023, 1, 3, 0, 0, 0, TimeSpan.Zero);
        TomlFileRateCache cache = CreateCache(RateCacheFileLayout.Monthly);
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.50m, januaryCachedAt) }, Duration, januaryCachedAt);
        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-01.toml")));

        // Store a February row well past the January row's 24-hour freshness, so the merge prunes the stale January row
        // and the reconcile deletes its now-empty partition file.
        DateTimeOffset februaryCachedAt = januaryCachedAt.AddDays(40);
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 2, 6), 0.51m, februaryCachedAt) }, Duration, februaryCachedAt);

        Assert.IsFalse(File.Exists(Path.Combine(PairDirectory, "2023-01.toml")), "the pruned January partition file is removed");
        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023-02.toml")), "the fresh February partition file remains");
        Assert.HasCount(1, cache.GetRates(Pair, Duration, februaryCachedAt));
    }

    /// <summary>
    /// Verifies that a yearly layout writes one file per calendar year, keyed by the four-digit year.
    /// </summary>
    [TestMethod]
    public void Store_WhenLayoutIsYearly_ShouldWriteOneFilePerYear()
    {
        TomlFileRateCache cache = CreateCache(RateCacheFileLayout.Yearly);
        DateTimeOffset now = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        cache.Store(
            Pair,
            new[]
            {
                new CachedRate(new DateOnly(2022, 6, 1), 0.50m, now),
                new CachedRate(new DateOnly(2023, 6, 1), 0.51m, now),
            },
            Duration,
            now);

        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2022.toml")), "the 2022 partition file is written");
        Assert.IsTrue(File.Exists(Path.Combine(PairDirectory, "2023.toml")), "the 2023 partition file is written");
    }
}

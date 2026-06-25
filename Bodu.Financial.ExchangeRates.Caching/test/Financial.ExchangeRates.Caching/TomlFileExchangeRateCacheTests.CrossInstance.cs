// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.CrossInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that two independent <see cref="TomlFileExchangeRateCache" /> instances sharing one cache directory — the
/// cross-process case, where no in-process per-pair lock is shared — never corrupt the store: the atomic temp-and-move
/// write keeps each pair file internally consistent, so a reader never observes coverage without its rows.
/// </summary>
public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that many concurrent same-pair range writes from two separate instances leave the shared file
    /// internally consistent: exactly one row for the date, its rate one of the writers', and the window covered.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenTwoInstancesWriteSamePairConcurrently_ShouldKeepFileConsistent()
    {
        var cacheA = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });
        var cacheB = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });
        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Two independent instances (no shared in-process lock) write the same pair and date with different rates,
        // concurrently and repeatedly, modelling two processes sharing one cache directory.
        List<Task> writes = new();
        for (int i = 0; i < 50; i++)
        {
            TomlFileExchangeRateCache cache = (i % 2 == 0) ? cacheA : cacheB;
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedExchangeRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        // A fresh instance reads the final persisted state. Because each write is an atomic temp-and-move of a complete
        // rows-plus-coverage blob, the reader never observes a torn file.
        var reader = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory });
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, Duration, now);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, now);

        Assert.HasCount(1, rows, "exactly one merged row survives, never a torn or duplicated set");
        Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
        Assert.IsTrue(coverage.Contains(date, date), "coverage is present with its row, never recorded without it");
    }
}

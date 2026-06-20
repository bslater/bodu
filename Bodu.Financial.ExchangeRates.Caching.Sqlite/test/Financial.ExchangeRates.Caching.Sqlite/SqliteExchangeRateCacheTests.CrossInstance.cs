// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.CrossInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that two independent <see cref="SqliteExchangeRateCache" /> instances sharing one database file — the
/// cross-process case, where no in-process per-pair lock is shared — never corrupt the store: SQLite's own locking and
/// the per-write transaction keep each pair's state internally consistent, so a reader never observes coverage without
/// its rows.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that many concurrent same-pair range writes from two separate instances over one database file leave the
    /// pair's state internally consistent: a recorded coverage window always has its row, and the surviving rate is one
    /// of the writers'.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenTwoInstancesWriteSamePairConcurrently_ShouldKeepStateConsistent()
    {
        string path = NewDatabasePath();
        var cacheA = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        var cacheB = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cacheA);
        _caches.Add(cacheB);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Two independent instances write the same pair and date with different rates, concurrently and repeatedly. A
        // write that loses the database lock degrades to a swallowed failure (best-effort), but never corrupts the store.
        List<Task> writes = new();
        for (int i = 0; i < 40; i++)
        {
            SqliteExchangeRateCache cache = (i % 2 == 0) ? cacheA : cacheB;
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedExchangeRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        // The first uncontended write establishes the row and its coverage; no write removes them, so the final state is
        // exactly one row whose rate is one writer's, with the window covered — never a torn coverage-without-rows state.
        IReadOnlyList<CachedExchangeRate> rows = cacheA.GetRates(Pair, Duration, now);
        DateRangeCoverage coverage = cacheA.GetCoverage(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count, "exactly one merged row survives, never a torn or duplicated set");
        Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
        Assert.IsTrue(coverage.Contains(date, date), "coverage is present with its row, never recorded without it");
    }
}

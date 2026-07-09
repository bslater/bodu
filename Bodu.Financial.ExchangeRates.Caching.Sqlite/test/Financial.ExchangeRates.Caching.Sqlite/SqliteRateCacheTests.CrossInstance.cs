// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.CrossInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that two independent <see cref="SqliteRateCache" /> instances sharing one database file — the
/// cross-process case, where no in-process per-pair lock is shared — never corrupt the store: SQLite's own locking and
/// the per-write transaction keep each pair's state internally consistent, so a reader never observes coverage without
/// its rows.
/// </summary>
public sealed partial class SqliteRateCacheTests
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
        var cacheA = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        var cacheB = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cacheA);
        _caches.Add(cacheB);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Two independent instances write the same pair and date with different rates, concurrently and repeatedly. A
        // write that loses the database lock degrades to a swallowed failure (best-effort), but never corrupts the store.
        List<Task> writes = new();
        for (int i = 0; i < 40; i++)
        {
            SqliteRateCache cache = (i % 2 == 0) ? cacheA : cacheB;
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        // The first uncontended write establishes the row and its coverage; no write removes them, so the final state is
        // exactly one row whose rate is one writer's, with the window covered — never a torn coverage-without-rows state.
        IReadOnlyList<CachedRate> rows = cacheA.GetRates(Pair, Duration, now);
        DateRangeCoverage coverage = cacheA.GetCoverage(Pair, Duration, now);

        Assert.HasCount(1, rows, "exactly one merged row survives, never a torn or duplicated set");
        Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
        Assert.IsTrue(coverage.Contains(date, date), "coverage is present with its row, never recorded without it");
    }

    /// <summary>
    /// Verifies that two caches bound to different providers but sharing one database file keep their series partitioned:
    /// each reads back only its own provider's rate for the same pair and date, confirming the provider key column
    /// isolates the series with no cross-contamination.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenTwoProvidersShareOneFile_ShouldKeepSeriesPartitioned()
    {
        string path = NewDatabasePath();
        var rba = new SqliteRateCache(new SqliteRateCacheOptions { Provider = "RBA", DatabaseFilePath = path });
        var ofx = new SqliteRateCache(new SqliteRateCacheOptions { Provider = "OFX", DatabaseFilePath = path });
        _caches.Add(rba);
        _caches.Add(ofx);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Both providers store the same pair and date with different rates into the one shared file.
        rba.Store(Pair, new[] { new CachedRate(date, 0.5000m, now) }, Duration, now);
        ofx.Store(Pair, new[] { new CachedRate(date, 0.6000m, now) }, Duration, now);

        IReadOnlyList<CachedRate> rbaRows = rba.GetRates(Pair, Duration, now);
        IReadOnlyList<CachedRate> ofxRows = ofx.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rbaRows, "the RBA cache sees exactly its own row, not the OFX row");
        Assert.HasCount(1, ofxRows, "the OFX cache sees exactly its own row, not the RBA row");
        Assert.AreEqual(0.5000m, rbaRows[0].Rate, "the RBA cache reads back only its own provider's rate");
        Assert.AreEqual(0.6000m, ofxRows[0].Rate, "the OFX cache reads back only its own provider's rate");
    }

    /// <summary>
    /// Verifies that coverage windows are partitioned by provider in a shared file: a window recorded by one provider is
    /// invisible to a second provider that recorded nothing, confirming the provider key column isolates the coverage
    /// table just as it does the rates table.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenTwoProvidersShareOneFile_ShouldKeepCoveragePartitioned()
    {
        string path = NewDatabasePath();
        SqliteRateCache rba = CreateFileCache("RBA", path);
        SqliteRateCache ofx = CreateFileCache("OFX", path);

        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 10);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        rba.RecordCoverage(Pair, start, end, Duration, now);

        Assert.IsTrue(rba.GetCoverage(Pair, Duration, now).Contains(start, end), "the recording provider sees its window");
        Assert.IsTrue(ofx.GetCoverage(Pair, Duration, now).IsEmpty, "the other provider sees no coverage for the pair");
    }

    /// <summary>
    /// Verifies that <see cref="SqliteRateCache.StoreFetchedRange" /> keeps both the rate and coverage halves
    /// partitioned by provider in a shared file, so each provider reads back only its own rows and windows.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenTwoProvidersShareOneFile_ShouldKeepBothHalvesPartitioned()
    {
        string path = NewDatabasePath();
        SqliteRateCache rba = CreateFileCache("RBA", path);
        SqliteRateCache ofx = CreateFileCache("OFX", path);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        rba.StoreFetchedRange(Pair, new[] { new CachedRate(date, 0.5000m, now) }, date, date, Duration, now);
        ofx.StoreFetchedRange(Pair, new[] { new CachedRate(date, 0.6000m, now) }, date, date, Duration, now);

        IReadOnlyList<CachedRate> rbaRows = rba.GetRates(Pair, Duration, now);
        IReadOnlyList<CachedRate> ofxRows = ofx.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rbaRows, "each provider's rate half is isolated");
        Assert.HasCount(1, ofxRows, "each provider's rate half is isolated");
        Assert.AreEqual(0.5000m, rbaRows[0].Rate);
        Assert.AreEqual(0.6000m, ofxRows[0].Rate);
        Assert.IsTrue(rba.GetCoverage(Pair, Duration, now).Contains(date, date), "each provider's coverage half is isolated");
        Assert.IsTrue(ofx.GetCoverage(Pair, Duration, now).Contains(date, date), "each provider's coverage half is isolated");
    }

    /// <summary>
    /// Verifies that two providers whose names differ only by letter case are stored as distinct series, since the
    /// provider key is matched with SQLite's default case-sensitive text comparison.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenProvidersDifferOnlyByCase_ShouldTreatAsDistinctSeries()
    {
        string path = NewDatabasePath();
        SqliteRateCache lower = CreateFileCache("ofx", path);
        SqliteRateCache upper = CreateFileCache("OFX", path);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lower.Store(Pair, new[] { new CachedRate(date, 0.5000m, now) }, Duration, now);
        upper.Store(Pair, new[] { new CachedRate(date, 0.6000m, now) }, Duration, now);

        Assert.AreEqual(0.5000m, lower.GetRates(Pair, Duration, now)[0].Rate, "the lower-case provider keeps its own series");
        Assert.AreEqual(0.6000m, upper.GetRates(Pair, Duration, now)[0].Rate, "the upper-case provider keeps its own series");
    }

    /// <summary>
    /// Verifies that many concurrent same-pair writes from two instances bound to different providers over one file
    /// preserve both providers' series: the writes never collide because the provider is part of the key, so each
    /// provider's row survives.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenDifferentProvidersWriteSamePairConcurrently_ShouldKeepBothSeries()
    {
        string path = NewDatabasePath();
        SqliteRateCache rba = CreateFileCache("RBA", path);
        SqliteRateCache ofx = CreateFileCache("OFX", path);

        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<Task> writes = new();
        for (int i = 0; i < 40; i++)
        {
            SqliteRateCache cache = (i % 2 == 0) ? rba : ofx;
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        IReadOnlyList<CachedRate> rbaRows = rba.GetRates(Pair, Duration, now);
        IReadOnlyList<CachedRate> ofxRows = ofx.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rbaRows, "the RBA series survives independently of the OFX writers");
        Assert.HasCount(1, ofxRows, "the OFX series survives independently of the RBA writers");
        Assert.AreEqual(0.5000m, rbaRows[0].Rate);
        Assert.AreEqual(0.6000m, ofxRows[0].Rate);
    }

    /// <summary>
    /// Creates a cache bound to <paramref name="provider" /> over <paramref name="path" />, tracking it for cleanup.
    /// </summary>
    /// <param name="provider">The provider the cache stores rates for.</param>
    /// <param name="path">The shared database file path the cache opens.</param>
    /// <returns>A new cache bound to <paramref name="provider" /> over <paramref name="path" />.</returns>
    private SqliteRateCache CreateFileCache(string provider, string path)
    {
        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = provider, DatabaseFilePath = path });
        _caches.Add(cache);
        return cache;
    }
}

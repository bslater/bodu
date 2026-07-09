// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Financial.Currencies;
using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the connection-level concurrency settings applied by <see cref="SqliteExchangeRateCache" /> on open — the
/// write-ahead logging mode that lets caches and processes share one file safely.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a file-backed cache with write-ahead logging enabled switches the database file to WAL journal
    /// mode, which a separate connection observes from the persisted file header.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenWriteAheadLoggingEnabled_ShouldSetFileJournalModeToWal()
    {
        string path = NewDatabasePath();
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions
        {
            Provider = Provider,
            DatabaseFilePath = path,
            UseWriteAheadLogging = true,
        });
        _caches.Add(cache);

        Assert.AreEqual("wal", ReadJournalMode(path));
    }

    /// <summary>
    /// Verifies that a file-backed cache with write-ahead logging disabled leaves the database in its default
    /// (non-WAL) journal mode.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenWriteAheadLoggingDisabled_ShouldLeaveFileJournalModeNonWal()
    {
        string path = NewDatabasePath();
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions
        {
            Provider = Provider,
            DatabaseFilePath = path,
            UseWriteAheadLogging = false,
        });
        _caches.Add(cache);

        Assert.AreNotEqual("wal", ReadJournalMode(path));
    }

    /// <summary>
    /// Verifies that enabling write-ahead logging over a shared in-memory database — which cannot honor WAL — is applied
    /// best-effort: construction does not throw, the database stays in its native <c>memory</c> journal mode, and rates
    /// still round-trip.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSharedInMemoryDatabaseWithWal_ShouldNotThrowAndLeaveMemoryJournalMode()
    {
        string connectionString = $"Data Source=bodu-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions
        {
            Provider = Provider,
            ConnectionString = connectionString,
            UseWriteAheadLogging = true,
        });
        _caches.Add(cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        // A second connection to the same shared in-memory database observes its journal mode.
        using var probe = new SqliteConnection(connectionString);
        probe.Open();
        using SqliteCommand command = probe.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        string mode = ((string)command.ExecuteScalar()!).ToLowerInvariant();

        Assert.AreEqual("memory", mode, "an in-memory database keeps its native journal mode rather than switching to WAL");
        Assert.HasCount(1, cache.GetRates(Pair, Duration, now), "the rate still round-trips through the WAL-requested in-memory cache");
    }

    /// <summary>
    /// Verifies that a non-default busy timeout is accepted and the cache still stores and serves rates, exercising the
    /// <c>busy_timeout</c> pragma path with a configured value.
    /// </summary>
    [TestMethod]
    public void Store_WhenCustomBusyTimeoutConfigured_ShouldStoreAndRead()
    {
        string path = NewDatabasePath();
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions
        {
            Provider = Provider,
            DatabaseFilePath = path,
            BusyTimeout = TimeSpan.FromSeconds(1),
        });
        _caches.Add(cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.HasCount(1, cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that a zero busy timeout — which disables waiting on a held lock — still allows an uncontended store and
    /// read to succeed.
    /// </summary>
    [TestMethod]
    public void Store_WhenBusyTimeoutIsZero_ShouldStoreAndRead()
    {
        string path = NewDatabasePath();
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions
        {
            Provider = Provider,
            DatabaseFilePath = path,
            BusyTimeout = TimeSpan.Zero,
        });
        _caches.Add(cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.HasCount(1, cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that a single shared cache instance — the way it is registered in DI — handles concurrent writes to
    /// different pairs without losing any: every pair written in parallel reads back its own rate, exercising the
    /// per-pair lock granularity and the per-operation connections under contention.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task Store_WhenManyThreadsWriteDifferentPairsOnOneInstance_ShouldPersistEvery()
    {
        SqliteExchangeRateCache cache = CreateFileCache();
        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CurrencyPair[] pairs =
        {
            new(CurrencyCode.AUD, CurrencyCode.USD),
            new(CurrencyCode.GBP, CurrencyCode.USD),
            new(CurrencyCode.EUR, CurrencyCode.USD),
            new(CurrencyCode.JPY, CurrencyCode.USD),
            new(CurrencyCode.CAD, CurrencyCode.USD),
            new(CurrencyCode.CHF, CurrencyCode.USD),
            new(CurrencyCode.NZD, CurrencyCode.USD),
            new(CurrencyCode.SGD, CurrencyCode.USD),
        };

        // Each pair is written from its own task concurrently against the one shared instance.
        List<Task> writes = new();
        for (int i = 0; i < pairs.Length; i++)
        {
            CurrencyPair pair = pairs[i];
            decimal rate = 0.5000m + (0.0100m * i);
            writes.Add(Task.Run(() => cache.Store(pair, new[] { new CachedExchangeRate(date, rate, now) }, Duration, now)));
        }

        await Task.WhenAll(writes);

        // Every pair must have persisted its own rate; none lost to lock contention or a swallowed busy failure.
        for (int i = 0; i < pairs.Length; i++)
        {
            IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(pairs[i], Duration, now);

            Assert.HasCount(1, rows, $"pair {pairs[i].From}{pairs[i].To} persisted its row");
            Assert.AreEqual(0.5000m + (0.0100m * i), rows[0].Rate, $"pair {pairs[i].From}{pairs[i].To} kept its own rate");
        }
    }

    /// <summary>
    /// Verifies that many concurrent same-pair range writes against a single shared cache instance leave the pair's
    /// state internally consistent: the per-pair lock serializes them so exactly one merged row survives with its
    /// coverage window, never a torn or duplicated set.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenManyThreadsWriteSamePairOnOneInstance_ShouldKeepStateConsistent()
    {
        SqliteExchangeRateCache cache = CreateFileCache();
        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<Task> writes = new();
        for (int i = 0; i < 64; i++)
        {
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedExchangeRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, now);

        Assert.HasCount(1, rows, "exactly one merged row survives, never a torn or duplicated set");
        Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
        Assert.IsTrue(coverage.Contains(date, date), "coverage is present with its row");
    }

    /// <summary>
    /// Verifies that reads running concurrently with writes against a single shared instance never throw and always
    /// observe a consistent value: each read returns either no row or a row carrying one of the written rates, which the
    /// write-ahead logging mode allows without blocking the writer.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task GetRates_WhenReadConcurrentlyWithWrites_ShouldNeverThrowAndStayConsistent()
    {
        SqliteExchangeRateCache cache = CreateFileCache();
        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        decimal[] rates = { 0.5000m, 0.6000m, 0.7000m };

        ConcurrentBag<Exception> errors = new();
        ConcurrentBag<decimal> observed = new();

        List<Task> work = new();
        for (int i = 0; i < 32; i++)
        {
            decimal rate = rates[i % rates.Length];
            work.Add(Task.Run(() =>
            {
                try
                {
                    cache.Store(Pair, new[] { new CachedExchangeRate(date, rate, now) }, Duration, now);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));
            work.Add(Task.Run(() =>
            {
                try
                {
                    IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
                    if (rows.Count > 0)
                        observed.Add(rows[0].Rate);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));
        }

        await Task.WhenAll(work);

        Assert.IsEmpty(errors, "neither concurrent reads nor writes surface an exception");
        foreach (decimal rate in observed)
            Assert.Contains(rate, rates, "a concurrent read only ever observes a written rate, never a torn value");
    }

    /// <summary>
    /// Reads the persisted journal mode of the database file through an independent connection.
    /// </summary>
    /// <param name="path">The database file path to inspect.</param>
    /// <returns>The lower-case journal-mode name reported by the database.</returns>
    private static string ReadJournalMode(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";

        return ((string)command.ExecuteScalar()!).ToLowerInvariant();
    }
}

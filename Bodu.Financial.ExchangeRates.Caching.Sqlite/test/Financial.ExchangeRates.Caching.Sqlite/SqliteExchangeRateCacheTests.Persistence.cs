// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.Persistence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteExchangeRateCache" /> persists its state to the database so it survives across
/// separate cache instances pointing at the same file.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that rates stored through one cache instance are readable through a second instance opened on the same
    /// database file after the first is disposed.
    /// </summary>
    [TestMethod]
    public void Store_WhenSecondInstanceOpensSameFile_ShouldReadPersistedRates()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that coverage recorded through one cache instance is readable through a second instance opened on the
    /// same database file after the first is disposed.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenSecondInstanceOpensSameFile_ShouldReadPersistedCoverage()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, now);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that both the rate and coverage halves persist together so a reopened instance sees the full state.
    /// </summary>
    [TestMethod]
    public void StoreAndRecordCoverage_WhenSecondInstanceOpensSameFile_ShouldReadBothHalves()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);

        Assert.AreEqual(1, reader.GetRates(Pair, Duration, now).Count);
        Assert.IsTrue(reader.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that a connection string targeting a shared in-memory database is honoured and serves a stored rate
    /// while at least one connection is held open.
    /// </summary>
    [TestMethod]
    public void Store_WhenSharedInMemoryConnectionString_ShouldServeRate()
    {
        var now = DateTimeOffset.UtcNow;
        var connectionString = $"Data Source=bodu-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, ConnectionString = connectionString });
        _caches.Add(cache);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }
}

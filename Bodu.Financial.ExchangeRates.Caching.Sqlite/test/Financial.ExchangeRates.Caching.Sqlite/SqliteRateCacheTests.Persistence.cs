// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.Persistence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteRateCache" /> persists its state to the database so it survives across
/// separate cache instances pointing at the same file.
/// </summary>
public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that rates stored through one cache instance are readable through a second instance opened on the same
    /// database file after the first is disposed.
    /// </summary>
    [TestMethod]
    public void Store_WhenSecondInstanceOpensSameFile_ShouldReadPersistedRates()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();

        SqliteRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        writer.Dispose();

        SqliteRateCache reader = CreateFileCache(path);
        IReadOnlyList<CachedRate> rows = reader.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();

        SqliteRateCache writer = CreateFileCache(path);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);
        writer.Dispose();

        SqliteRateCache reader = CreateFileCache(path);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, now);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that both the rate and coverage halves persist together so a reopened instance sees the full state.
    /// </summary>
    [TestMethod]
    public void StoreAndRecordCoverage_WhenSecondInstanceOpensSameFile_ShouldReadBothHalves()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();

        SqliteRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);
        writer.Dispose();

        SqliteRateCache reader = CreateFileCache(path);

        Assert.HasCount(1, reader.GetRates(Pair, Duration, now));
        Assert.IsTrue(reader.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that a connection string targeting a shared in-memory database is honoured and serves a stored rate
    /// while at least one connection is held open.
    /// </summary>
    [TestMethod]
    public void Store_WhenSharedInMemoryConnectionString_ShouldServeRate()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string connectionString = $"Data Source=bodu-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, ConnectionString = connectionString });
        _caches.Add(cache);

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that opening a cache over a database created by a pre-C build — whose <c>rates</c> table lacks the
    /// <c>observed_at</c> column — migrates the schema in place, then serves the legacy row with a <see langword="null" />
    /// <see cref="CachedRate.ObservedAtUtc" /> and without throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenDatabaseHasLegacyRatesSchema_ShouldMigrateAndReadRowWithNullObservedAtUtc()
    {
        string path = NewDatabasePath();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Build a database with the OLD rates schema (no observed_at column) and seed one row directly, modelling a
        // database written by a pre-C build of the cache.
        SeedLegacyDatabase(path, new DateOnly(2023, 1, 3), 0.5000m, now);

        // Opening the cache over it runs EnsureSchema, which probes for observed_at and adds it via ALTER TABLE.
        SqliteRateCache cache = CreateFileCache(path);
        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.AreEqual(0.5000m, rows[0].Rate);
        Assert.IsNull(rows[0].ObservedAtUtc);
    }

    /// <summary>
    /// Verifies that after a legacy database is migrated, a subsequent store of a row carrying an upstream fetch instant
    /// persists it through the freshly added <c>observed_at</c> column and reads it back intact.
    /// </summary>
    [TestMethod]
    public void Store_WhenDatabaseMigratedFromLegacySchema_ShouldPersistObservedAtUtc()
    {
        string path = NewDatabasePath();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var observedAt = new DateTimeOffset(2023, 1, 6, 16, 0, 0, TimeSpan.Zero);
        SeedLegacyDatabase(path, new DateOnly(2023, 1, 3), 0.5000m, now);

        SqliteRateCache cache = CreateFileCache(path);
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 6), 0.5100m, now, observedAt) }, Duration, now);

        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(2, rows);

        CachedRate migrated = rows.Single(r => r.Date == new DateOnly(2023, 1, 6));
        Assert.AreEqual(observedAt, migrated.ObservedAtUtc);
    }

    /// <summary>
    /// Creates a SQLite database at <paramref name="path" /> with the pre-C <c>rates</c> schema (no <c>observed_at</c>
    /// column) and inserts one row for the tests' provider and pair.
    /// </summary>
    /// <param name="path">The database file path to create.</param>
    /// <param name="date">The observation date of the seeded row.</param>
    /// <param name="rate">The rate of the seeded row.</param>
    /// <param name="cachedAt">The cache-write instant of the seeded row.</param>
    private static void SeedLegacyDatabase(string path, DateOnly date, decimal rate, DateTimeOffset cachedAt)
    {
        using SqliteConnection connection = new($"Data Source={path}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS rates (
                provider   TEXT NOT NULL,
                from_code  TEXT NOT NULL,
                to_code    TEXT NOT NULL,
                obs_date   TEXT NOT NULL,
                rate       TEXT NOT NULL,
                cached_at  TEXT NOT NULL,
                PRIMARY KEY (provider, from_code, to_code, obs_date)
            );

            INSERT INTO rates (provider, from_code, to_code, obs_date, rate, cached_at)
            VALUES ($provider, $from, $to, $date, $rate, $cached);
            """;
        command.Parameters.AddWithValue("$provider", Provider);
        command.Parameters.AddWithValue("$from", Pair.From.ToString());
        command.Parameters.AddWithValue("$to", Pair.To.ToString());
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$rate", rate.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$cached", cachedAt.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}

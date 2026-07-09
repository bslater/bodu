// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.Resilience.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteRateCache" /> degrades gracefully on storage failure, surfacing an empty
/// read or a skipped write rather than an exception, as required by the <see cref="IRateCache" /> contract.
/// </summary>
public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that constructing a cache over a file whose contents are not a valid SQLite database does not throw.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFileIsNotADatabase_ShouldNotThrow()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        Assert.AreEqual(Provider, cache.Provider);
    }

    /// <summary>
    /// Verifies that a read against a corrupt database returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenDatabaseIsCorrupt_ShouldReturnEmpty()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Verifies that a coverage read against a corrupt database returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenDatabaseIsCorrupt_ShouldReturnEmpty()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsTrue(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that a write against a corrupt database is a silent no-op rather than throwing.
    /// </summary>
    [TestMethod]
    public void Store_WhenDatabaseIsCorrupt_ShouldBeNoOp()
    {
        string path = NewCorruptDatabaseFile();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        // The store must not throw, and the subsequent read must still report nothing.
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.IsEmpty(cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that recording coverage against a corrupt database is a silent no-op rather than throwing, while still
    /// validating its arguments.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenDatabaseIsCorrupt_ShouldBeNoOp()
    {
        string path = NewCorruptDatabaseFile();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).IsEmpty);
    }

    /// <summary>
    /// Verifies that a persisted rate row whose value is beyond the range of <see cref="decimal" /> is skipped on
    /// read, returning empty rather than letting the resulting <see cref="OverflowException" /> escape the documented
    /// best-effort read contract.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetRates_WhenRowHasOutOfRangeDecimalRate_ShouldReturnEmptyNotThrow()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();
        SqliteRateCache cache = CreateFileCache(path);
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        // Poison the stored rate with a value that has more digits than decimal can represent, so ParseRate throws
        // OverflowException when the row is read back.
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using Microsoft.Data.Sqlite.SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE rates SET rate = '99999999999999999999999999999999';";
            command.ExecuteNonQuery();
        }

        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Creates a temporary file whose contents are deliberately not a valid SQLite database, tracking it for cleanup.
    /// </summary>
    /// <returns>The path to a corrupt database file.</returns>
    private string NewCorruptDatabaseFile()
    {
        string path = NewDatabasePath();
        File.WriteAllText(path, "this is not a sqlite database file");
        return path;
    }
}

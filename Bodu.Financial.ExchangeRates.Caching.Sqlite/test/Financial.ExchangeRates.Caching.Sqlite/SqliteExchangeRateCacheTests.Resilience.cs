// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.Resilience.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteExchangeRateCache" /> degrades gracefully on storage failure, surfacing an empty
/// read or a skipped write rather than an exception, as required by the <see cref="IExchangeRateCache" /> contract.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that constructing a cache over a file whose contents are not a valid SQLite database does not throw.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenFileIsNotADatabase_ShouldNotThrow()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
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

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, rows.Count);
    }

    /// <summary>
    /// Verifies that a coverage read against a corrupt database returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenDatabaseIsCorrupt_ShouldReturnEmpty()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
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

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        // The store must not throw, and the subsequent read must still report nothing.
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.AreEqual(0, cache.GetRates(Pair, Duration, now).Count);
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

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).IsEmpty);
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

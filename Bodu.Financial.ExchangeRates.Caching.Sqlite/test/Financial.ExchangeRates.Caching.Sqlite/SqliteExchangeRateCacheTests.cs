// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the SQLite-specific behaviour of <see cref="SqliteExchangeRateCache" /> beyond the shared cache contract:
/// schema creation, persistence across instances, storage-failure resilience, and lossless round-tripping.
/// </summary>
[TestClass]
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// The provider the caches under test are bound to.
    /// </summary>
    private const string Provider = "Test";

    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly ExchangeRatePair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// The freshness duration used by the tests.
    /// </summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// The temporary database files created during a test, removed on cleanup.
    /// </summary>
    private readonly List<string> _files = new();

    /// <summary>
    /// The caches created during a test, disposed on cleanup so their database handles are released.
    /// </summary>
    private readonly List<SqliteExchangeRateCache> _caches = new();

    /// <summary>
    /// Removes every temporary database file and disposes every cache created during the test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        foreach (SqliteExchangeRateCache cache in _caches)
            cache.Dispose();

        foreach (string file in _files)
        {
            // Remove the database and any WAL sidecar files (-wal / -shm) left when write-ahead logging is enabled.
            foreach (string candidate in new[] { file, file + "-wal", file + "-shm" })
            {
                try
                {
                    if (File.Exists(candidate))
                        File.Delete(candidate);
                }
                catch (IOException)
                {
                    // Best-effort cleanup.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup.
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a freshly constructed cache over a new database file serves a stored rate, confirming the schema is
    /// created on first use.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void SqliteExchangeRateCache_WhenStoredAndReadBack_ShouldServeRate()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SqliteExchangeRateCache cache = CreateFileCache();

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that constructing a cache over a brand-new database file does not throw, creating the schema lazily.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseFileDoesNotExist_ShouldCreateSchemaWithoutThrowing()
    {
        SqliteExchangeRateCache cache = CreateFileCache();

        // A read against the freshly created (empty) schema must succeed and report nothing.
        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Verifies that the bound provider is reported from the options.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldReportBoundProvider()
    {
        SqliteExchangeRateCache cache = CreateFileCache();

        Assert.AreEqual(Provider, cache.Provider);
    }

    /// <summary>
    /// Verifies that disposing the cache twice is a safe no-op, so the keep-alive connection is never disposed more
    /// than once.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        SqliteExchangeRateCache cache = CreateFileCache();

        cache.Dispose();

        // A second dispose must not throw; the idempotent guard means only the first call releases the connection.
        cache.Dispose();
    }

    /// <summary>
    /// Creates a cache over a fresh temporary database file, tracking both for cleanup.
    /// </summary>
    /// <returns>A new cache bound to <see cref="Provider" /> over a unique file.</returns>
    private SqliteExchangeRateCache CreateFileCache() =>
        CreateFileCache(NewDatabasePath());

    /// <summary>
    /// Creates a cache over the supplied database file path, tracking the cache for cleanup.
    /// </summary>
    /// <param name="path">The database file path the cache opens.</param>
    /// <returns>A new cache bound to <see cref="Provider" /> over <paramref name="path" />.</returns>
    private SqliteExchangeRateCache CreateFileCache(string path)
    {
        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);
        return cache;
    }

    /// <summary>
    /// Builds a unique temporary database file path, tracking it for cleanup.
    /// </summary>
    /// <returns>A unique database file path under the system temporary directory.</returns>
    private string NewDatabasePath()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-exchange-rates-sqlite-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _files.Add(path);
        return path;
    }
}

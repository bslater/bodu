// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.ConnectionReuse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that write-ahead logging, applied once at construction, persists on the database so per-operation
    /// connections inherit it without re-asserting the pragma.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenWriteAheadLoggingEnabled_ShouldPersistWalOnDatabase()
    {
        string path = NewDatabasePath();
        _ = CreateFileCache(path);

        // An independent connection with no pragma of its own must observe the persisted journal mode.
        using var probe = new SqliteConnection($"Data Source={path}");
        probe.Open();
        using SqliteCommand command = probe.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";

        Assert.AreEqual("wal", (string?)command.ExecuteScalar(), "the journal mode must persist at the database level");
    }

    /// <summary>
    /// Verifies that a range store still round-trips correctly through the consolidated single-connection,
    /// single-transaction path: rows and coverage land together and a covered read serves them.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenConsolidatedTransaction_ShouldPersistRowsAndCoverageTogether()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SqliteRateCache cache = CreateFileCache();
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 6);

        RateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            new[] { new CachedRate(start, 0.5m, now), new CachedRate(end, 0.51m, now) },
            start,
            end,
            Duration,
            now);

        Assert.AreEqual(RateCacheWriteStatus.Stored, status);
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(start, end));
        Assert.HasCount(2, cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies range-store atomicity under a read-only database: the write reports
    /// <see cref="RateCacheWriteStatus.Failed" /> and neither rows nor coverage are partially persisted.
    /// </summary>
    /// <remarks>
    /// Read-only enforcement uses SQLite's own <c>Mode=ReadOnly</c> rather than a file-system attribute, which an
    /// elevated test process could bypass. The schema is created by a normal cache over the same file first.
    /// </remarks>
    [TestMethod]
    public void StoreFetchedRange_WhenDatabaseReadOnly_ShouldFailWithoutPartialState()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();
        _ = CreateFileCache(path);
        SqliteConnection.ClearAllPools();

        using var cache = new SqliteRateCache(new SqliteRateCacheOptions
        {
            Provider = Provider,
            ConnectionString = $"Data Source={path};Mode=ReadOnly",
        });
        RateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 6),
            Duration,
            now);

        Assert.AreEqual(RateCacheWriteStatus.Failed, status, "a read-only database must surface as a failed write");
        Assert.IsEmpty(cache.GetRates(Pair, Duration, now), "no rows may be partially persisted");
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).IsEmpty, "no coverage may be partially persisted");
    }

    /// <summary>
    /// Verifies that a shared in-memory database still works across operations, pinning the keep-alive connection
    /// contract after the connection consolidation.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenSharedInMemoryDatabase_ShouldPersistAcrossOperations()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var options = new SqliteRateCacheOptions
        {
            Provider = Provider,
            ConnectionString = $"Data Source=reuse-{Guid.NewGuid():N};Mode=Memory;Cache=Shared",
        };
        using var cache = new SqliteRateCache(options);
        var date = new DateOnly(2023, 1, 3);

        cache.StoreFetchedRange(Pair, new[] { new CachedRate(date, 0.5m, now) }, date, date, Duration, now);

        Assert.HasCount(1, cache.GetRates(Pair, Duration, now), "the keep-alive connection must preserve the shared in-memory database between operations");
    }
}

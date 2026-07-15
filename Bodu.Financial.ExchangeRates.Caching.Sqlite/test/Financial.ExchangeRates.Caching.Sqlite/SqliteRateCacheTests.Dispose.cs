// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that disposing the cache releases every pooled connection to its database file: the write-ahead-log
    /// sidecar is checkpointed away — which SQLite does only when the <em>last</em> connection closes — and the
    /// database file is immediately deletable.
    /// </summary>
    /// <remarks>
    /// Regression guard for the Windows failure where <c>Microsoft.Data.Sqlite</c> connection pooling kept file
    /// handles open past <see cref="SqliteRateCache.Dispose" />, so deleting the database threw
    /// <see cref="IOException" />. On Linux deleting an open file succeeds silently, so the WAL-sidecar assertion
    /// carries the cross-platform signal while the delete carries it on Windows.
    /// </remarks>
    [TestMethod]
    public void Dispose_WhenFileBacked_ShouldReleaseEveryPooledConnection()
    {
        string path = NewDatabasePath();
        SqliteRateCache cache = CreateFileCache(path);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, now) }, Duration, now);
        Assert.IsTrue(File.Exists(path + "-wal"), "sanity: write-ahead logging must have created the WAL sidecar");

        cache.Dispose();

        Assert.IsFalse(File.Exists(path + "-wal"), "disposal must close the last pooled connection, checkpointing and removing the WAL sidecar");
        File.Delete(path);
        Assert.IsFalse(File.Exists(path), "the database file must be deletable immediately after disposal");
    }
}

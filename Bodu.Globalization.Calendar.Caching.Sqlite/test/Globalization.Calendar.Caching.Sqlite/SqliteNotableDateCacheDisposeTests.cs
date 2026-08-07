// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheDisposeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching.Sqlite;

/// <summary>
/// Verifies that disposing <see cref="SqliteNotableDateCache" /> releases every connection to its database file, so
/// the file is deletable immediately afterwards.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheDisposeTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The time-to-live used by the tests.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    /// <summary>
    /// Verifies that disposing the cache releases every pooled connection to its database file: the write-ahead-log
    /// sidecar is checkpointed away — which SQLite does only when the <em>last</em> connection closes — and the
    /// database file is immediately deletable.
    /// </summary>
    /// <remarks>
    /// Regression guard for the Windows failure where <c>Microsoft.Data.Sqlite</c> connection pooling kept file
    /// handles open past <see cref="SqliteNotableDateCache.Dispose" />, so the contract tests' cleanup threw
    /// <see cref="IOException" /> deleting the temporary database. On Linux deleting an open file succeeds silently,
    /// so the WAL-sidecar assertion carries the cross-platform signal while the delete carries it on Windows.
    /// </remarks>
    [TestMethod]
    public void Dispose_WhenFileBacked_ShouldReleaseEveryPooledConnection()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var cache = new SqliteNotableDateCache(new SqliteNotableDateCacheOptions { DatabaseFilePath = path });
        _ = cache.StoreYear(new NotableDateCacheEntry("US", 2026, "v1", new[] { NotableDateCacheTestFactory.NewYearsDay(2026) }, Now), Ttl, Now);
        Assert.IsTrue(File.Exists(path + "-wal"), "sanity: write-ahead logging must have created the WAL sidecar");

        cache.Dispose();

        Assert.IsFalse(File.Exists(path + "-wal"), "disposal must close the last pooled connection, checkpointing and removing the WAL sidecar");
        File.Delete(path);
        Assert.IsFalse(File.Exists(path), "the database file must be deletable immediately after disposal");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheWalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that write-ahead logging, applied once to the keep-alive connection at construction, persists at the
/// database level so per-operation connections inherit it without re-asserting the pragma.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheWalTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that the journal mode persists on the database file after construction.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenWriteAheadLoggingEnabled_ShouldPersistWalOnDatabase()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-wal-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            using (var cache = new SqliteNotableDateCache(path))
            {
                cache.StoreYear(
                    new NotableDateCacheEntry("US", 2026, "v1", new[] { NotableDateCacheTestFactory.NewYearsDay(2026) }, Now),
                    TimeSpan.FromDays(30),
                    Now);
            }

            SqliteConnection.ClearAllPools();

            // An independent connection with no pragma of its own must observe the persisted journal mode.
            using var probe = new SqliteConnection($"Data Source={path}");
            probe.Open();
            using SqliteCommand command = probe.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";

            Assert.AreEqual("wal", (string?)command.ExecuteScalar(), "the journal mode must persist at the database level");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
            }
        }
    }
}

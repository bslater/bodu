// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

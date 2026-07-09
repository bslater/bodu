// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the opt-in strict-failure behaviour of <see cref="SqliteRateCache" />: when
/// <see cref="RateCacheOptions.ValidateStorageOnStart" /> or
/// <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is set, a database that cannot be opened or initialized
/// surfaces from the constructor rather than degrading to empty reads and skipped writes.
/// </summary>
public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that constructing the cache with startup validation over a file that is not a valid database throws,
    /// surfacing the unusable store at construction.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseUnusableAndValidateStorageOnStart_ShouldThrow()
    {
        string path = NewCorruptDatabaseFile();

        _ = Assert.ThrowsExactly<SqliteException>(() =>
        {
            _ = new SqliteRateCache(new SqliteRateCacheOptions
            {
                Provider = Provider,
                DatabaseFilePath = path,
                ValidateStorageOnStart = true,
            });
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with strict failure over a file that is not a valid database throws, since a
    /// strict-runtime cache must not start over a database it cannot initialize.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseUnusableAndThrowOnStorageFailure_ShouldThrow()
    {
        string path = NewCorruptDatabaseFile();

        _ = Assert.ThrowsExactly<SqliteException>(() =>
        {
            _ = new SqliteRateCache(new SqliteRateCacheOptions
            {
                Provider = Provider,
                DatabaseFilePath = path,
                ThrowOnStorageFailure = true,
            });
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with startup validation over a usable path succeeds, so the probe is a no-op
    /// when the database can be created and initialized.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseUsableAndValidateStorageOnStart_ShouldNotThrow()
    {
        string path = NewDatabasePath();

        var cache = new SqliteRateCache(new SqliteRateCacheOptions
        {
            Provider = Provider,
            DatabaseFilePath = path,
            ValidateStorageOnStart = true,
        });
        _caches.Add(cache);

        Assert.AreEqual(Provider, cache.Provider);
    }
}

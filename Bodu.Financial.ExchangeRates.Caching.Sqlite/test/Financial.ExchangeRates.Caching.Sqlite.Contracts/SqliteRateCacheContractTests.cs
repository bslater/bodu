// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching.Contracts;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite.Contracts;

/// <summary>
/// Runs the shared <see cref="IRateCache" /> contract against the SQLite cache, each test using an isolated
/// temporary database file that is removed on cleanup. Confirms the SQLite backend is behaviourally identical to the
/// in-memory and TOML caches.
/// </summary>
[TestClass]
public sealed class SqliteRateCacheContractTests
    : RateCacheContractTests<SqliteRateCache>
{
    /// <summary>
    /// The caches created during a test, disposed on cleanup so their database handles are released.
    /// </summary>
    private readonly List<SqliteRateCache> _caches = new();

    /// <summary>
    /// The temporary database files created during a test, removed on cleanup.
    /// </summary>
    private readonly List<string> _files = new();

    /// <inheritdoc />
    protected override SqliteRateCache CreateCache()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-exchange-rates-sqlite-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _files.Add(path);

        var cache = new SqliteRateCache(new SqliteRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);
        return cache;
    }

    /// <summary>
    /// Disposes every cache and removes every temporary database file created during the test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        foreach (SqliteRateCache cache in _caches)
            cache.Dispose();

        foreach (string file in _files)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
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

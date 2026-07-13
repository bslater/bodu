// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Bodu.Globalization.Calendar.Caching.Contracts;

namespace Bodu.Globalization.Calendar.Caching.Sqlite.Contracts;

/// <summary>
/// Runs the shared <see cref="INotableDateCache" /> contract against the SQLite cache, each test using an isolated
/// temporary database file that is removed on cleanup, confirming the SQLite backend is behaviourally identical to the
/// in-memory and file caches.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheContractTests
    : NotableDateCacheContractTests
{
    /// <summary>The caches created during a test, disposed on cleanup so their database handles are released.</summary>
    private readonly List<SqliteNotableDateCache> _caches = new();

    /// <summary>The temporary database files created during a test, removed on cleanup.</summary>
    private readonly List<string> _files = new();

    /// <summary>
    /// Disposes the caches created during a test and removes their database files.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        foreach (SqliteNotableDateCache cache in _caches)
            cache.Dispose();

        foreach (string file in _files)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    /// <inheritdoc />
    protected override INotableDateCache CreateCache()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _files.Add(path);

        var cache = new SqliteNotableDateCache(new SqliteNotableDateCacheOptions { DatabaseFilePath = path });
        _caches.Add(cache);
        return cache;
    }
}

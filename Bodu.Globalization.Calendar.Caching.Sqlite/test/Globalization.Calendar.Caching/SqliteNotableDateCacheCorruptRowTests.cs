// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheCorruptRowTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="SqliteNotableDateCache" /> treats a corrupt stored row — an unparsable occurrences JSON
/// blob or computed instant — as a skipped row rather than a thrown exception, preserving the best-effort read
/// contract even when <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is set.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheCorruptRowTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The freshness duration applied by every test.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    /// <summary>The per-test database file, removed on cleanup.</summary>
    private string _path = null!;

    /// <summary>
    /// Creates a unique database path and seeds it with valid cached years 2026 and 2027 for territory US.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-corrupt-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        using var cache = new SqliteNotableDateCache(_path);
        cache.StoreYear(new NotableDateCacheEntry("US", 2026, "v1", new[] { NotableDateCacheTestFactory.NewYearsDay(2026) }, Now), Ttl, Now);
        cache.StoreYear(new NotableDateCacheEntry("US", 2027, "v1", new[] { NotableDateCacheTestFactory.NewYearsDay(2027) }, Now), Ttl, Now);
    }

    /// <summary>
    /// Removes the per-test database file.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    /// <summary>
    /// Verifies that a row whose occurrences blob is not JSON is skipped: its year reads as a miss, the sibling valid
    /// year is still served, and no exception escapes.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenOccurrencesBlobIsNotJson_ShouldSkipRowAndServeValidRows()
    {
        CorruptColumn("occurrences", "{ this is not json", year: 2026);

        using var cache = new SqliteNotableDateCache(_path);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now), "the corrupt year must read as a miss");
        NotableDateCacheEntry? valid = cache.GetYear("US", 2027, "v1", Ttl, Now);
        Assert.IsNotNull(valid);
        Assert.HasCount(1, valid.Occurrences);
    }

    /// <summary>
    /// Verifies that a truncated occurrences blob — valid JSON tokens cut off mid-array — is skipped without throwing.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenOccurrencesBlobIsTruncated_ShouldSkipRow()
    {
        CorruptColumn("occurrences", "[{\"notableDateId\":\"new-years-day\"", year: 2026);

        using var cache = new SqliteNotableDateCache(_path);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a row whose computed instant cannot be parsed is skipped without throwing.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenComputedInstantIsUnparsable_ShouldSkipRow()
    {
        CorruptColumn("computed_at", "not-an-instant", year: 2026);

        using var cache = new SqliteNotableDateCache(_path);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that row corruption is treated as data damage rather than a storage failure: even with
    /// <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> set, a corrupt occurrences blob degrades to a
    /// skipped row instead of throwing.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenOccurrencesBlobIsNotJsonAndThrowOnStorageFailure_ShouldSkipRowWithoutThrowing()
    {
        CorruptColumn("occurrences", "{ this is not json", year: 2026);

        using var cache = new SqliteNotableDateCache(new SqliteNotableDateCacheOptions
        {
            DatabaseFilePath = _path,
            ThrowOnStorageFailure = true,
        });

        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now));
        Assert.IsNotNull(cache.GetYear("US", 2027, "v1", Ttl, Now));
    }

    /// <summary>
    /// Verifies that a store for a new year — which reads and merges the territory's existing rows — succeeds when one
    /// stored row is corrupt, dropping the corrupt row and keeping the valid ones.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenExistingRowIsCorrupt_ShouldStoreAndPreserveValidRows()
    {
        CorruptColumn("occurrences", "{ this is not json", year: 2026);

        using var cache = new SqliteNotableDateCache(_path);
        NotableDateCacheWriteStatus status = cache.StoreYear(
            new NotableDateCacheEntry("US", 2028, "v1", new[] { NotableDateCacheTestFactory.NewYearsDay(2028) }, Now), Ttl, Now);

        Assert.AreEqual(NotableDateCacheWriteStatus.Stored, status);
        Assert.IsNotNull(cache.GetYear("US", 2027, "v1", Ttl, Now), "the valid sibling year must survive the merge");
        Assert.IsNotNull(cache.GetYear("US", 2028, "v1", Ttl, Now));
        Assert.IsNull(cache.GetYear("US", 2026, "v1", Ttl, Now), "the corrupt row is dropped by the rewrite");
    }

    /// <summary>
    /// Overwrites one column of a seeded row with corrupt text through a direct connection, bypassing the cache.
    /// </summary>
    /// <param name="column">The column to overwrite.</param>
    /// <param name="value">The corrupt text to store.</param>
    /// <param name="year">The seeded year whose row is corrupted.</param>
    private void CorruptColumn(string column, string value, int year)
    {
        using var connection = new SqliteConnection($"Data Source={_path}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"UPDATE notable_dates SET {column} = $value WHERE territory = 'US' AND year = $year;";
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$year", year);

        Assert.AreEqual(1, command.ExecuteNonQuery(), "the seeded row must exist to be corrupted");
    }
}

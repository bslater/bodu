// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheMetricsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;
using Microsoft.Data.Sqlite;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="SqliteNotableDateCache" /> counts every swallowed storage failure through the
/// <c>Bodu.Globalization.Calendar.Caching.Sqlite</c> meter, outside the rate-limited warning gate.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheMetricsTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a swallowed write failure against a read-only database increments the storage-failure counter,
    /// once per swallow even when the warning gate suppresses the log.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenDatabaseReadOnly_ShouldCountEverySwallowedFailure()
    {
        string path = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-sqlite-metrics-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            using (var seeder = new SqliteNotableDateCache(path))
                seeder.StoreYear(NotableDateCacheTestFactory.Entry("US", 2025, "v1", Now), TimeSpan.FromDays(30), Now);
            SqliteConnection.ClearAllPools();

            using var collector = new TestMeterCollector(CalendarCachingSqliteMeter.MeterName);
            using var cache = new SqliteNotableDateCache(new SqliteNotableDateCacheOptions
            {
                ConnectionString = $"Data Source={path};Mode=ReadOnly",
            });
            long before = collector.Sum("bodu.calendar.notable_date_cache.sqlite.storage_failures", "operation", "store");

            for (int i = 0; i < 3; i++)
                cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), TimeSpan.FromDays(30), Now);

            long after = collector.Sum("bodu.calendar.notable_date_cache.sqlite.storage_failures", "operation", "store");
            Assert.IsTrue(after - before >= 3, "every swallowed failure must be counted, outside the log rate limiting");
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

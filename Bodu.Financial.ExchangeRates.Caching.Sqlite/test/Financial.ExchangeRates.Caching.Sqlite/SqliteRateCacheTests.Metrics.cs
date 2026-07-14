// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheTests.Metrics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteRateCacheTests
{
    /// <summary>
    /// Verifies that a swallowed storage failure — a write against a read-only database — increments the
    /// storage-failure counter with the provider tag.
    /// </summary>
    [TestMethod]
    public void Store_WhenDatabaseReadOnly_ShouldCountSwallowedFailure()
    {
        string provider = $"metrics-{Guid.NewGuid():N}";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string path = NewDatabasePath();
        _ = CreateFileCache(path);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        using var collector = new TestMeterCollector(CachingSqliteMeter.MeterName);
        using var cache = new SqliteRateCache(new SqliteRateCacheOptions
        {
            Provider = provider,
            ConnectionString = $"Data Source={path};Mode=ReadOnly",
        });

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, now) }, Duration, now);

        Assert.IsTrue(
            collector.Sum("bodu.financial.rate_cache.sqlite.storage_failures", "provider", provider) >= 1,
            "the swallowed read-only write must be counted");
    }
}

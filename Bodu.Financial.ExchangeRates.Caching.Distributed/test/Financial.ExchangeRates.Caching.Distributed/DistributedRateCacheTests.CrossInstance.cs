// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.CrossInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies the cross-process behaviour of <see cref="DistributedRateCache" /> using two independent instances
/// over one shared backing store — the multi-writer case any real <see cref="IDistributedCache" /> (Redis, SQL Server)
/// presents. Same-pair writes are last-write-wins across instances, but because a fetched range is one atomic blob set a
/// reader never observes coverage without its rows even when a concurrent writer overwrites it.
/// </summary>
public sealed partial class DistributedRateCacheTests
{
    /// <summary>
    /// Verifies that many concurrent same-pair range writes from two instances over one backing store leave the per-pair
    /// blob internally consistent: one row whose rate is a writer's, with the window covered — never a torn blob.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenTwoInstancesShareOneBackingStore_ShouldKeepBlobConsistent()
    {
        // One backing store shared by two cache instances models two processes (for example two app servers over one
        // Redis): each has its own in-process per-pair lock, so cross-instance writes are last-write-wins.
        IDistributedCache backingStore = CreateBackingStore();
        var cacheA = new DistributedRateCache(backingStore, new DistributedRateCacheOptions { Provider = Provider });
        var cacheB = new DistributedRateCache(backingStore, new DistributedRateCacheOptions { Provider = Provider });
        var date = new DateOnly(2023, 1, 3);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<Task> writes = new();
        for (int i = 0; i < 50; i++)
        {
            DistributedRateCache cache = (i % 2 == 0) ? cacheA : cacheB;
            decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
            writes.Add(Task.Run(() => cache.StoreFetchedRange(
                Pair, new[] { new CachedRate(date, rate, now) }, date, date, Duration, now)));
        }

        await Task.WhenAll(writes);

        // A reader on the other instance observes one writer's complete rows-plus-coverage blob, never coverage alone.
        IReadOnlyList<CachedRate> rows = cacheB.GetRates(Pair, Duration, now);
        DateRangeCoverage coverage = cacheB.GetCoverage(Pair, Duration, now);

        Assert.HasCount(1, rows, "exactly one merged row survives, never a torn or duplicated set");
        Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
        Assert.IsTrue(coverage.Contains(date, date), "coverage is present with its row, never recorded without it");
    }
}

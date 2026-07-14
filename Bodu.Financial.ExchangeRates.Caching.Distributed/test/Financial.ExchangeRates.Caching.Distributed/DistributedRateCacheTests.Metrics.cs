// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.Metrics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheTests
{
    /// <summary>
    /// Verifies that every swallowed storage failure increments the storage-failure counter — even the ones whose
    /// warning log is suppressed by the rate-limiting gate — so sustained degradation is quantifiable.
    /// </summary>
    [TestMethod]
    public void Store_WhenBackingStoreThrowsRepeatedly_ShouldCountEverySwallowedFailure()
    {
        string provider = $"metrics-{Guid.NewGuid():N}";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using var collector = new TestMeterCollector(CachingDistributedMeter.MeterName);
        var cache = new DistributedRateCache(new ThrowingDistributedCache(), new DistributedRateCacheOptions { Provider = provider });

        for (int i = 0; i < 3; i++)
            cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, now) }, Duration, now);

        // Three swallowed failures (one per store: the read inside the read-merge-write throws first), each counted
        // even though the warning gate logs at most one per cooldown window.
        Assert.IsTrue(
            collector.Sum("bodu.financial.rate_cache.distributed.storage_failures", "provider", provider) >= 3,
            "every swallowed failure must be counted, outside the log rate limiting");
    }
}

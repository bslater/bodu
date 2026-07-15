// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheMetricsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies that <see cref="DistributedNotableDateCache" /> counts every swallowed storage failure through the
/// <c>Bodu.Globalization.Calendar.Caching.Distributed</c> meter, outside the rate-limited warning gate.
/// </summary>
[TestClass]
public sealed class DistributedNotableDateCacheMetricsTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that repeated swallowed failures against an unreachable backing store are each counted, even while the
    /// warning gate suppresses all but the first log.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenBackingStoreThrowsRepeatedly_ShouldCountEverySwallowedFailure()
    {
        using var collector = new TestMeterCollector(CalendarCachingDistributedMeter.MeterName);
        var cache = new DistributedNotableDateCache(new ThrowingCalendarDistributedCache(), new DistributedNotableDateCacheOptions());
        long before = collector.Sum("bodu.calendar.notable_date_cache.distributed.storage_failures", "operation", "read");

        for (int i = 0; i < 3; i++)
            cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), TimeSpan.FromDays(30), Now);

        // Each store's read-merge-write swallows the read fault first, so three stores count at least three failures.
        long after = collector.Sum("bodu.calendar.notable_date_cache.distributed.storage_failures", "operation", "read");
        Assert.IsTrue(after - before >= 3, "every swallowed failure must be counted, outside the log rate limiting");
    }

    /// <summary>
    /// An <see cref="IDistributedCache" /> whose every operation throws, standing in for an unreachable store.
    /// </summary>
    private sealed class ThrowingCalendarDistributedCache : IDistributedCache
    {
        /// <inheritdoc />
        public byte[]? Get(string key) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public void Refresh(string key) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public Task RefreshAsync(string key, CancellationToken token = default) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public void Remove(string key) => throw new InvalidOperationException("The distributed cache is unavailable.");

        /// <inheritdoc />
        public Task RemoveAsync(string key, CancellationToken token = default) => throw new InvalidOperationException("The distributed cache is unavailable.");
    }
}

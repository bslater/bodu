// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies the opt-in strict-failure behaviour of <see cref="DistributedRateCache" />: when
/// <see cref="RateCacheOptions.ThrowOnStorageFailure" /> or
/// <see cref="RateCacheOptions.ValidateStorageOnStart" /> is set, a backing-store fault is surfaced rather than
/// degraded, so a deployment can fail fast instead of running with a silently broken cache.
/// </summary>
public sealed partial class DistributedRateCacheTests
{
    /// <summary>
    /// Verifies that a read against a failing backing store rethrows the fault when strict failure is enabled, rather
    /// than degrading to an empty result.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBackingStoreFailsAndStrict_ShouldThrow()
    {
        DistributedRateCache cache = new(
            new FailingDistributedCache(),
            new DistributedRateCacheOptions { Provider = Provider, ThrowOnStorageFailure = true });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);
        });
    }

    /// <summary>
    /// Verifies that an atomic range write against a failing backing store rethrows the fault when strict failure is
    /// enabled, rather than reporting a swallowed failure.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenBackingStoreFailsAndStrict_ShouldThrow()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DistributedRateCache cache = new(
            new FailingDistributedCache(),
            new DistributedRateCacheOptions { Provider = Provider, ThrowOnStorageFailure = true });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.StoreFetchedRange(
                Pair,
                new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
                new DateOnly(2023, 1, 3),
                new DateOnly(2023, 1, 3),
                Duration,
                now);
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with startup validation against a failing backing store throws, surfacing an
    /// unreachable distributed cache at construction rather than on the first read or write.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenValidateStorageOnStartAndBackingStoreFails_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new DistributedRateCache(
                new FailingDistributedCache(),
                new DistributedRateCacheOptions { Provider = Provider, ValidateStorageOnStart = true });
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with startup validation against a healthy backing store succeeds, so the
    /// probe is a no-op when the store is reachable.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenValidateStorageOnStartAndBackingStoreHealthy_ShouldNotThrow()
    {
        DistributedRateCache cache = new(
            CreateBackingStore(),
            new DistributedRateCacheOptions { Provider = Provider, ValidateStorageOnStart = true });

        Assert.AreEqual(Provider, cache.Provider);
    }
}

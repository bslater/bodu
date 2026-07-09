// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RedisDistributedRateCacheIntegrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Exercises <see cref="DistributedRateCache" /> against a live Redis server, covering the network round-trip
/// and multi-writer race the in-memory <c>MemoryDistributedCache</c> tests cannot reproduce. The tests are
/// environment-gated: they run only when <c>BODU_TEST_REDIS</c> holds a Redis connection string, and report inconclusive
/// otherwise so the default build needs no Redis.
/// </summary>
[TestClass]
public sealed class RedisDistributedRateCacheIntegrationTests
{
    /// <summary>
    /// The environment variable holding the Redis connection string the tests connect to.
    /// </summary>
    private const string RedisConnectionEnvVar = "BODU_TEST_REDIS";

    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// The freshness duration used by the tests.
    /// </summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// Verifies that an atomic range write persists and reads back through a live Redis store, exercising the real
    /// network serialization round-trip end to end.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void StoreFetchedRange_OverLiveRedis_ShouldRoundTripThroughTheStore()
    {
        string? connection = Environment.GetEnvironmentVariable(RedisConnectionEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Inconclusive($"Set {RedisConnectionEnvVar} to a Redis connection string to run the live Redis integration test.");
            return;
        }

        string keyPrefix = $"bodu-test:{Guid.NewGuid():N}:";
        using var redis = new RedisCache(Options.Create(new RedisCacheOptions { Configuration = connection }));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);
        var cache = new DistributedRateCache(redis, new DistributedRateCacheOptions { Provider = "RedisTest", KeyPrefix = keyPrefix });

        try
        {
            RateCacheWriteStatus status = cache.StoreFetchedRange(
                Pair, new[] { new CachedRate(date, 0.5m, now) }, date, date, Duration, now);

            Assert.AreEqual(RateCacheWriteStatus.Stored, status);

            IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, now);
            Assert.HasCount(1, rows);
            Assert.AreEqual(0.5m, rows[0].Rate);
            Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(date, date));
        }
        finally
        {
            redis.Remove($"{keyPrefix}RedisTest:AUDUSD");
        }
    }

    /// <summary>
    /// Verifies that many concurrent same-pair range writes from two instances over one live Redis store leave the
    /// per-pair blob internally consistent — one row whose rate is a writer's, with the window covered — proving the
    /// last-write-wins blob set holds under real network races, not just an in-memory store.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task StoreFetchedRange_WhenTwoInstancesRaceOverLiveRedis_ShouldKeepBlobConsistent()
    {
        string? connection = Environment.GetEnvironmentVariable(RedisConnectionEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Inconclusive($"Set {RedisConnectionEnvVar} to a Redis connection string to run the live Redis integration test.");
            return;
        }

        string keyPrefix = $"bodu-test:{Guid.NewGuid():N}:";
        using var redis = new RedisCache(Options.Create(new RedisCacheOptions { Configuration = connection }));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);
        var cacheA = new DistributedRateCache(redis, new DistributedRateCacheOptions { Provider = "RedisTest", KeyPrefix = keyPrefix });
        var cacheB = new DistributedRateCache(redis, new DistributedRateCacheOptions { Provider = "RedisTest", KeyPrefix = keyPrefix });

        try
        {
            List<Task> writes = new();
            for (int i = 0; i < 50; i++)
            {
                DistributedRateCache cache = (i % 2 == 0) ? cacheA : cacheB;
                decimal rate = (i % 2 == 0) ? 0.5000m : 0.6000m;
                writes.Add(Task.Run(() => cache.StoreFetchedRange(
                    Pair, new[] { new CachedRate(date, rate, now) }, date, date, Duration, now)));
            }

            await Task.WhenAll(writes);

            IReadOnlyList<CachedRate> rows = cacheB.GetRates(Pair, Duration, now);
            Assert.HasCount(1, rows, "exactly one merged row survives, never a torn or duplicated set");
            Assert.IsTrue(rows[0].Rate == 0.5000m || rows[0].Rate == 0.6000m, "the surviving rate is one writer's, not a mix");
            Assert.IsTrue(cacheB.GetCoverage(Pair, Duration, now).Contains(date, date), "coverage is present with its row, never recorded without it");
        }
        finally
        {
            redis.Remove($"{keyPrefix}RedisTest:AUDUSD");
        }
    }
}

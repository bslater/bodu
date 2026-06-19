// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheExtensionsTests.AddRedisRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection;

public sealed partial class DistributedRateCacheExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddRedisRateCache</c> registers a Redis <see cref="IDistributedCache" /> together with
    /// the exchange-rate cache, asserting service registration without requiring a live Redis server.
    /// </summary>
    [TestMethod]
    public void AddRedisRateCache_WhenRegistered_ShouldRegisterDistributedCacheAndExchangeRateCache()
    {
        var services = new ServiceCollection();

        services.AddBoduFinancial().AddRedisRateCache(redis => redis.Configuration = "localhost:6379", "RBA");

        // AddStackExchangeRedisCache registers IDistributedCache; the builder registers IExchangeRateCache (default and
        // keyed) and the concrete cache. Assert the descriptors exist without resolving the Redis cache (which would
        // attempt a connection).
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IDistributedCache)));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IExchangeRateCache) && d.ServiceKey is null));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IExchangeRateCache) && Equals(d.ServiceKey, "RBA")));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(DistributedExchangeRateCache)));
    }

    /// <summary>
    /// Verifies that <c>AddRedisRateCache</c> rejects a <see langword="null" /> Redis configuration callback.
    /// </summary>
    [TestMethod]
    public void AddRedisRateCache_WhenConfigureRedisIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddRedisRateCache(null!, "RBA");
        });

        Assert.AreEqual("configureRedis", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the cache resolved through <c>AddRedisRateCache</c> is the bound provider's cache, using an
    /// in-memory distributed cache substituted for Redis so no server is required.
    /// </summary>
    [TestMethod]
    public void AddRedisRateCache_WhenDistributedCacheSubstituted_ShouldResolveCacheBoundToProvider()
    {
        var services = new ServiceCollection();
        services.AddBoduFinancial().AddRedisRateCache(redis => redis.Configuration = "localhost:6379", "RBA");

        // Replace the Redis IDistributedCache registration with an in-memory one so the cache can be resolved and used
        // without a live Redis server, while leaving the exchange-rate cache wiring under test intact.
        services.RemoveAll<IDistributedCache>();
        services.AddDistributedMemoryCache();

        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", cache.Provider);
    }
}

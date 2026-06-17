// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection;

/// <summary>
/// Verifies the dependency-injection wiring of the distributed (Redis-capable) exchange-rate cache.
/// </summary>
[TestClass]
public sealed class DistributedExchangeRateCacheServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that the registered cache resolves as an <see cref="IExchangeRateCache" /> bound to the supplied
    /// provider when an <see cref="IDistributedCache" /> is already registered.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddDistributedExchangeRateCache_WhenRegistered_ShouldResolveCacheBoundToProvider()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA");
        });

        var cache = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", cache.Provider);
    }

    /// <summary>
    /// Verifies that the registered cache is also resolvable as a keyed service under the provider name and is the same
    /// singleton as the default registration.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenRegistered_ShouldResolveKeyedSameInstance()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA");
        });

        var byDefault = provider.GetRequiredService<IExchangeRateCache>();
        var byKey = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");

        Assert.AreSame(byDefault, byKey);
    }

    /// <summary>
    /// Verifies that the resolved cache persists and serves rates through the registered <see cref="IDistributedCache" />.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenResolved_ShouldPersistThroughDistributedCache()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA");
        });
        var cache = provider.GetRequiredService<IExchangeRateCache>();
        var now = DateTimeOffset.UtcNow;

        cache.Store(new ExchangeRatePair("AUD", "USD"), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.AreEqual(1, cache.GetRates(new ExchangeRatePair("AUD", "USD"), TimeSpan.FromHours(24), now).Count);
    }

    /// <summary>
    /// Verifies that the key prefix is bound from configuration so the cache writes under the prefixed key.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenConfigurationProvided_ShouldBindKeyPrefix()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Financial:ExchangeRateCache:Distributed:KeyPrefix"] = "fx:" })
            .Build();

        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA", config);
        });
        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        var cache = provider.GetRequiredService<IExchangeRateCache>();
        var now = DateTimeOffset.UtcNow;

        cache.Store(new ExchangeRatePair("AUD", "USD"), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        // The configured "fx:" prefix must be applied to the underlying distributed-cache key.
        Assert.IsNotNull(distributedCache.Get("fx:RBA:AUDUSD"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected by <c>AddDistributedExchangeRateCache</c>.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DistributedExchangeRateCacheServiceBuilderExtensions.AddDistributedExchangeRateCache(null!, "RBA");
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank provider name is rejected by <c>AddDistributedExchangeRateCache</c>.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenProviderNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddDistributedExchangeRateCache("  ");
        });

        Assert.AreEqual("providerName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that invalid options — here, a white-space key prefix — fail fast through <c>ValidateOnStart</c> when
    /// the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA", configure: o => o.KeyPrefix = "   ");
        });

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IExchangeRateCache>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the cache.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenOptionsValid_ShouldResolveCache()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA", configure: o => o.KeyPrefix = "fx:");
        });

        Assert.IsNotNull(provider.GetRequiredService<IExchangeRateCache>());
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over an unreachable
    /// backing store, the startup validation the host runs fails, so an unreachable distributed cache fails the host
    /// start rather than the first lookup.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenValidateStorageOnStartAndStoreUnreachable_ShouldFailStartupValidation()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSingleton<IDistributedCache>(new ThrowingDistributedCache());
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA", configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        _ = Assert.ThrowsExactly<OptionsValidationException>(startup.Validate);
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over a reachable backing
    /// store, the startup validation the host runs passes.
    /// </summary>
    [TestMethod]
    public void AddDistributedExchangeRateCache_WhenValidateStorageOnStartAndStoreReachable_ShouldPassStartupValidation()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddBoduFinancial().AddDistributedExchangeRateCache("RBA", configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Verifies that <c>AddRedisExchangeRateCache</c> registers a Redis <see cref="IDistributedCache" /> together with
    /// the exchange-rate cache, asserting service registration without requiring a live Redis server.
    /// </summary>
    [TestMethod]
    public void AddRedisExchangeRateCache_WhenRegistered_ShouldRegisterDistributedCacheAndExchangeRateCache()
    {
        var services = new ServiceCollection();

        services.AddBoduFinancial().AddRedisExchangeRateCache(redis => redis.Configuration = "localhost:6379", "RBA");

        // AddStackExchangeRedisCache registers IDistributedCache; the builder registers IExchangeRateCache (default and
        // keyed) and the concrete cache. Assert the descriptors exist without resolving the Redis cache (which would
        // attempt a connection).
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IDistributedCache)));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IExchangeRateCache) && d.ServiceKey is null));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IExchangeRateCache) && Equals(d.ServiceKey, "RBA")));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(DistributedExchangeRateCache)));
    }

    /// <summary>
    /// Verifies that <c>AddRedisExchangeRateCache</c> rejects a <see langword="null" /> Redis configuration callback.
    /// </summary>
    [TestMethod]
    public void AddRedisExchangeRateCache_WhenConfigureRedisIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddRedisExchangeRateCache(null!, "RBA");
        });

        Assert.AreEqual("configureRedis", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the cache resolved through <c>AddRedisExchangeRateCache</c> is the bound provider's cache, using an
    /// in-memory distributed cache substituted for Redis so no server is required.
    /// </summary>
    [TestMethod]
    public void AddRedisExchangeRateCache_WhenDistributedCacheSubstituted_ShouldResolveCacheBoundToProvider()
    {
        var services = new ServiceCollection();
        services.AddBoduFinancial().AddRedisExchangeRateCache(redis => redis.Configuration = "localhost:6379", "RBA");

        // Replace the Redis IDistributedCache registration with an in-memory one so the cache can be resolved and used
        // without a live Redis server, while leaving the exchange-rate cache wiring under test intact.
        services.RemoveAll<IDistributedCache>();
        services.AddDistributedMemoryCache();

        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", cache.Provider);
    }

    /// <summary>
    /// Builds a service provider after applying the supplied registration against a fresh service collection.
    /// </summary>
    /// <param name="register">The registration callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        return services.BuildServiceProvider();
    }
}

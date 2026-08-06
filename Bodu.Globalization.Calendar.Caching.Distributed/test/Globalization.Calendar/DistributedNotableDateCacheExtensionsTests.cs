// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the dependency-injection wiring of the distributed notable-date cache.
/// </summary>
[TestClass]
public sealed class DistributedNotableDateCacheExtensionsTests
{
    /// <summary>
    /// Verifies that the registered cache resolves as an <see cref="INotableDateCache" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddDistributedNotableDateCache_WhenRegistered_ShouldResolveCache()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache();
        });

        INotableDateCache cache = provider.GetRequiredService<INotableDateCache>();

        Assert.IsInstanceOfType<DistributedNotableDateCache>(cache);
    }

    /// <summary>
    /// Verifies that the cache is registered as a singleton.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenResolvedTwice_ShouldReturnSameInstance()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache();
        });

        INotableDateCache first = provider.GetRequiredService<INotableDateCache>();
        INotableDateCache second = provider.GetRequiredService<INotableDateCache>();

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that options are bound from the supplied configuration section.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenConfigurationProvided_ShouldBindOptionsFromSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:NotableDateCache:Distributed:KeyPrefix"] = "holidays",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache(configuration);
        });

        DistributedNotableDateCacheOptions options =
            provider.GetRequiredService<IOptions<DistributedNotableDateCacheOptions>>().Value;

        Assert.AreEqual("holidays", options.KeyPrefix);
    }

    /// <summary>
    /// Verifies that the configure callback is applied after configuration binding, so code wins over
    /// configuration for the same setting.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenConfigureCallbackProvided_ShouldApplyAfterBinding()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:NotableDateCache:Distributed:KeyPrefix"] = "from-configuration",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache(configuration, configure: o => o.KeyPrefix = "from-code");
        });

        DistributedNotableDateCacheOptions options =
            provider.GetRequiredService<IOptions<DistributedNotableDateCacheOptions>>().Value;

        Assert.AreEqual("from-code", options.KeyPrefix);
    }

    /// <summary>
    /// Verifies that a non-default section name is honoured.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenSectionNameProvided_ShouldBindFromThatSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custom:Section:KeyPrefix"] = "custom",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache(configuration, "Custom:Section");
        });

        DistributedNotableDateCacheOptions options =
            provider.GetRequiredService<IOptions<DistributedNotableDateCacheOptions>>().Value;

        Assert.AreEqual("custom", options.KeyPrefix);
    }

    /// <summary>
    /// Verifies that a white-space key prefix fails validation when the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenKeyPrefixIsWhiteSpace_ShouldThrowOnResolve()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache(configure: o => o.KeyPrefix = "   ");
        });

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<INotableDateCache>();
        });
    }

    /// <summary>
    /// Verifies that the startup storage probe fails when the backing store is unreachable.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenValidateStorageOnStartAndStoreUnreachable_ShouldFailStartupValidation()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSingleton<IDistributedCache>(new ThrowingDistributedCache());
            services.AddDistributedNotableDateCache(configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        _ = Assert.ThrowsExactly<OptionsValidationException>(startup.Validate);
    }

    /// <summary>
    /// Verifies that the startup storage probe passes over a reachable backing store.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenValidateStorageOnStartAndStoreReachable_ShouldPassStartupValidation()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddDistributedNotableDateCache(configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Verifies that the storage probe is skipped when it has not been opted into, so an unreachable store does
    /// not fail startup for callers that never asked for the check.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenValidateStorageOnStartNotSet_ShouldSkipStorageProbe()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSingleton<IDistributedCache>(new ThrowingDistributedCache());
            services.AddDistributedNotableDateCache();
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Verifies that the registration returns the same service collection so calls can be chained.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_Always_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddDistributedNotableDateCache();

        Assert.AreSame(services, returned);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service collection throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenServicesIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DistributedNotableDateCacheExtensions.AddDistributedNotableDateCache(null!);
        });

        Assert.AreEqual("services", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank section name throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void AddDistributedNotableDateCache_WhenSectionNameIsWhiteSpace_ShouldThrowExactly()
    {
        var services = new ServiceCollection();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddDistributedNotableDateCache(sectionName: "   ");
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the Redis convenience overload registers an <see cref="IDistributedCache" /> alongside the
    /// notable-date cache. The descriptors are inspected rather than resolved so no Redis server is contacted.
    /// </summary>
    [TestMethod]
    public void AddRedisNotableDateCache_WhenRegistered_ShouldRegisterDistributedCacheAndNotableDateCache()
    {
        var services = new ServiceCollection();

        services.AddRedisNotableDateCache(redis => redis.Configuration = "localhost:6379");

        Assert.IsTrue(services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)));
        Assert.IsTrue(services.Any(descriptor => descriptor.ServiceType == typeof(INotableDateCache)));
    }

    /// <summary>
    /// Verifies that the Redis convenience overload applies the notable-date cache options it is given.
    /// </summary>
    [TestMethod]
    public void AddRedisNotableDateCache_WhenConfigureProvided_ShouldApplyNotableDateCacheOptions()
    {
        var services = new ServiceCollection();
        services.AddRedisNotableDateCache(
            redis => redis.Configuration = "localhost:6379",
            configure: o => o.KeyPrefix = "holidays");

        // Swap the Redis-backed store for an in-memory one so the options can be resolved without a server.
        services.RemoveAll<IDistributedCache>();
        services.AddDistributedMemoryCache();

        using ServiceProvider provider = services.BuildServiceProvider();
        DistributedNotableDateCacheOptions options =
            provider.GetRequiredService<IOptions<DistributedNotableDateCacheOptions>>().Value;

        Assert.AreEqual("holidays", options.KeyPrefix);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> Redis configuration callback throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddRedisNotableDateCache_WhenConfigureRedisIsNull_ShouldThrowExactly()
    {
        var services = new ServiceCollection();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddRedisNotableDateCache(null!);
        });

        Assert.AreEqual("configureRedis", ex.ParamName);
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

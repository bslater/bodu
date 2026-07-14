// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the fluent registration of a distributed (Redis-capable) notable-date cache onto an
/// <see cref="IServiceCollection" />.
/// </summary>
public static class DistributedNotableDateCacheExtensions
{
    /// <summary>The default configuration section bound into <see cref="DistributedNotableDateCacheOptions" />.</summary>
    private const string DefaultCacheSection = "Calendar:NotableDateCache:Distributed";

    /// <summary>
    /// Registers a <see cref="DistributedNotableDateCache" /> over the <see cref="IDistributedCache" /> already
    /// registered in the container, as the <see cref="INotableDateCache" /> used by the caching notable-date service.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="DistributedNotableDateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Calendar:NotableDateCache:Distributed</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for registering an <see cref="IDistributedCache" /> (for example via
    /// <c>AddStackExchangeRedisCache</c> or <c>AddDistributedMemoryCache</c>); use
    /// <see cref="AddRedisNotableDateCache" /> to register a Redis cache and the notable-date cache together. Compose
    /// it with the caching service by resolving the registered cache, for example
    /// <c>AddCachedNotableDateService(cacheFactory: sp =&gt; sp.GetRequiredService&lt;INotableDateCache&gt;())</c>.
    /// </remarks>
    public static IServiceCollection AddDistributedNotableDateCache(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<DistributedNotableDateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        OptionsBuilder<DistributedNotableDateCacheOptions> optionsBuilder = services.AddOptions<DistributedNotableDateCacheOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Distributed notable-date cache options are invalid.")
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DistributedNotableDateCacheOptions>, DistributedCacheStorageStartupValidator>());

        services.TryAddSingleton<INotableDateCache>(serviceProvider =>
        {
            DistributedNotableDateCacheOptions options = serviceProvider.GetRequiredService<IOptions<DistributedNotableDateCacheOptions>>().Value;
            IDistributedCache distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            return new DistributedNotableDateCache(
                distributedCache,
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<DistributedNotableDateCache>());
        });

        return services;
    }

    /// <summary>
    /// Registers a Redis-backed <see cref="IDistributedCache" /> and a <see cref="DistributedNotableDateCache" /> over
    /// it, as the <see cref="INotableDateCache" /> used by the caching notable-date service.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="configureRedis">The callback configuring the Redis cache connection and options.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="DistributedNotableDateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Calendar:NotableDateCache:Distributed</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> or <paramref name="configureRedis" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// A convenience over <see cref="AddDistributedNotableDateCache" /> that first registers a Redis
    /// <see cref="IDistributedCache" /> via <c>AddStackExchangeRedisCache</c>, then registers the notable-date cache
    /// over it.
    /// </remarks>
    public static IServiceCollection AddRedisNotableDateCache(
        this IServiceCollection services,
        Action<RedisCacheOptions> configureRedis,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<DistributedNotableDateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configureRedis);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddStackExchangeRedisCache(configureRedis);

        return services.AddDistributedNotableDateCache(configuration, sectionName, configure);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the fluent registration of a distributed (Redis-capable) exchange-rate cache onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class DistributedRateCacheExtensions
{
    /// <summary>The default configuration section bound into <see cref="DistributedRateCacheOptions" />.</summary>
    private const string DefaultCacheSection = "Financial:RateCache:Distributed";

    /// <summary>
    /// Registers a <see cref="DistributedRateCache" /> bound to <paramref name="providerName" /> over the
    /// <see cref="IDistributedCache" /> already registered in the container, resolvable as an <see cref="IRateCache" />
    /// and as a keyed <see cref="IRateCache" /> under the provider name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="providerName">The provider whose rates the cache stores.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="DistributedRateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:RateCache:Distributed</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName" /> or <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for registering an <see cref="IDistributedCache" /> (for example via
    /// <c>AddStackExchangeRedisCache</c> or <c>AddDistributedMemoryCache</c>); use <see cref="AddRedisRateCache" /> to
    /// register a Redis cache and the exchange-rate cache together. The cache is registered as a singleton so its
    /// per-pair write locks are shared across resolutions, and is exposed on both the default and the keyed
    /// <see cref="IRateCache" /> surface as the same instance. Options are validated through <c>ValidateOnStart</c>, so
    /// misconfiguration fails fast at application startup.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
    /// services.AddFinancialService()
    ///         .AddDistributedRateCache("RBA");
    ///
    /// // Resolve the cache, or wrap a source provider with a CachingRateProvider over it.
    /// var cache = provider.GetRequiredService<IRateCache>();
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddDistributedRateCache(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<DistributedRateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<DistributedRateCacheOptions> optionsBuilder =
            services.AddOptions<DistributedRateCacheOptions>(providerName);
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        optionsBuilder.Configure(options =>
        {
            options.Provider = providerName;
            configure?.Invoke(options);
        });
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Distributed exchange-rate cache options are invalid.")
            .ValidateOnStart();

        // Probe the backing store at host start when ValidateStorageOnStart is set, so an unreachable distributed cache
        // fails the start rather than the first lookup. The probe runs through the same ValidateOnStart wiring.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DistributedRateCacheOptions>, DistributedCacheStorageStartupValidator>());

        // Register the concrete cache once as a singleton so a single instance — and its per-pair locks — backs every
        // resolution. The backing IDistributedCache is resolved from the container so any registered distributed cache
        // (Redis, in-memory, SQL Server, …) can host it.
        services.TryAddSingleton(serviceProvider =>
        {
            DistributedRateCacheOptions options =
                serviceProvider.GetRequiredService<IOptionsMonitor<DistributedRateCacheOptions>>().Get(providerName);
            IDistributedCache distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            return new DistributedRateCache(distributedCache, options);
        });

        // Expose the same singleton on both the default and the keyed IRateCache surface so a specific cached
        // provider is resolvable by name without creating a second instance over the same distributed store.
        services.TryAddSingleton<IRateCache>(static serviceProvider => serviceProvider.GetRequiredService<DistributedRateCache>());
        services.TryAddKeyedSingleton<IRateCache>(providerName, static (serviceProvider, _) => serviceProvider.GetRequiredService<DistributedRateCache>());

        return builder;
    }

    /// <summary>
    /// Registers a Redis-backed <see cref="IDistributedCache" /> and a <see cref="DistributedRateCache" /> over it,
    /// bound to <paramref name="providerName" />, resolvable as an <see cref="IRateCache" /> and as a keyed
    /// <see cref="IRateCache" /> under the provider name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configureRedis">The callback configuring the Redis cache connection and options.</param>
    /// <param name="providerName">The provider whose rates the cache stores.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="DistributedRateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:RateCache:Distributed</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="configureRedis" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName" /> or <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// A convenience over <see cref="AddDistributedRateCache" /> that first registers a Redis
    /// <see cref="IDistributedCache" /> via <c>AddStackExchangeRedisCache</c>, then registers the exchange-rate cache
    /// over it. Use the lower-level <see cref="AddDistributedRateCache" /> directly when the
    /// <see cref="IDistributedCache" /> is registered separately or is not Redis.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddFinancialService()
    ///         .AddRedisRateCache("RBA", redis => redis.Configuration = "localhost:6379");
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddRedisRateCache(
        this IFinancialServiceBuilder builder,
        Action<RedisCacheOptions> configureRedis,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<DistributedRateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureRedis);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        // Register the Redis IDistributedCache, then bind the exchange-rate cache over whatever IDistributedCache is now
        // registered. AddStackExchangeRedisCache registers IDistributedCache as a singleton RedisCache.
        builder.Services.AddStackExchangeRedisCache(configureRedis);

        return builder.AddDistributedRateCache(providerName, configuration, sectionName, configure);
    }
}

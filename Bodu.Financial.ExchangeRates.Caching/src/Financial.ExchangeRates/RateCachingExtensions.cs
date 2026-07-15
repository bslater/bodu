// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides fluent registration of caching and aggregating exchange-rate providers onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class RateCachingExtensions
{
    /// <summary>The default configuration section bound into <see cref="CachingRateOptions" />.</summary>
    private const string DefaultCacheSection = "Financial:RateCache";

    /// <summary>
    /// Registers a <see cref="CachingRateProvider" /> that wraps a single source <typeparamref name="TProvider" /> over
    /// its own on-disk cache, resolvable as both <see cref="IDatedRateProvider" /> and the timeless
    /// <see cref="IRateProvider" />.
    /// </summary>
    /// <typeparam name="TProvider">The concrete source provider to cache.</typeparam>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="providerName">The name the source's rates are cached under.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="CachingRateOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:RateCache</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the <see cref="IRateCache" /> from the service provider and the provider name.
    /// When <see langword="null" />, a default <see cref="TomlFileRateCache" /> bound to
    /// <paramref name="providerName" /> under the options' <c>CacheDirectory</c> is used. Supply a factory to choose
    /// the storage structure — for example a JSON cache, a partitioned file layout, or a SQLite or distributed cache.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName" /> or <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The source <typeparamref name="TProvider" /> must already be registered — for example through its provider
    /// package's registration such as <c>AddRbaExchangeRates</c>. This method resolves the registered instance and
    /// wraps it in a caching decorator; it does not construct the source or its own dependencies (such as its
    /// <see cref="HttpClient" />), so registering only the cache without the source fails when the provider is
    /// resolved.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddFinancialService()
    ///         .AddRbaExchangeRates(configuration)
    ///         .AddCachedRateProvider<RbaRateProvider>("RBA", configuration,
    ///             configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
    ///
    /// // Consumers resolve IDatedRateProvider (or IRateProvider) and get cached lookups transparently.
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddCachedRateProvider<TProvider>(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<CachingRateOptions>? configure = null,
        Func<IServiceProvider, string, IRateCache>? cacheFactory = null)
        where TProvider : class, IDatedRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;
        BindCacheOptions(services, configuration, sectionName, configure);

        services.TryAddSingleton<TProvider>();
        services.AddSingleton(serviceProvider =>
            CreateCachingProvider(serviceProvider, providerName, serviceProvider.GetRequiredService<TProvider>(), cacheFactory));
        services.AddSingleton<IDatedRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingRateProvider>());
        services.AddSingleton<IRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingRateProvider>());

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="AggregatingRateProvider" /> that groups the cached children added through
    /// <paramref name="configure" />, resolvable as both <see cref="IDatedRateProvider" /> and the timeless
    /// <see cref="IRateProvider" />. Each child is also registered as a keyed <see cref="IDatedRateProvider" /> so a
    /// specific source can be resolved by name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configure">A callback that adds the cached children and configures routing and strategy.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into the shared <see cref="CachingRateOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:RateCache</c>.</param>
    /// <param name="configureCache">
    /// An optional callback applied to the shared cache options after configuration binding.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddFinancialService()
    ///         .AddAggregatedRateProvider(agg => agg
    ///             .AddCachedChild<RbaRateProvider>("RBA")
    ///             .AddCachedChild<EcbRateProvider>("ECB")
    ///             .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
    ///             .MapPair(new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP), "ECB", "RBA"));
    ///
    /// // Resolve the aggregate, or a specific source by name:
    /// var aggregate = provider.GetRequiredService<IDatedRateProvider>();
    /// var rbaOnly = provider.GetRequiredKeyedService<IDatedRateProvider>("RBA");
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddAggregatedRateProvider(
        this IFinancialServiceBuilder builder,
        Action<IAggregatedRateBuilder> configure,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<CachingRateOptions>? configureCache = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        AggregatedRateBuilder aggregateBuilder = new();
        configure(aggregateBuilder);

        IServiceCollection services = builder.Services;
        BindCacheOptions(services, configuration, sectionName, configureCache);

        // Register each child keyed by name so a specific cached source is resolvable through the service catalog.
        foreach ((string Name, Func<IServiceProvider, IDatedRateProvider> Factory, Func<IServiceProvider, string, IRateCache>? CacheFactory) child in aggregateBuilder.Children)
        {
            Func<IServiceProvider, IDatedRateProvider> factory = child.Factory;
            Func<IServiceProvider, string, IRateCache>? cacheFactory = child.CacheFactory;
            services.TryAddKeyedSingleton<IDatedRateProvider>(
                child.Name,
                (serviceProvider, key) => CreateCachingProvider(serviceProvider, (string)key!, factory(serviceProvider), cacheFactory));
        }

        string[] childNames = new string[aggregateBuilder.Children.Count];
        for (int i = 0; i < childNames.Length; i++)
            childNames[i] = aggregateBuilder.Children[i].Name;

        IReadOnlyList<(CurrencyPair Pair, string[] ProviderOrder, IRateAggregationStrategy? Strategy)> routes = aggregateBuilder.Routes;
        IRateAggregationStrategy? defaultStrategy = aggregateBuilder.DefaultStrategy;

        services.AddSingleton(serviceProvider =>
        {
            var children = new NamedDatedRateProvider[childNames.Length];
            for (int i = 0; i < childNames.Length; i++)
                children[i] = new NamedDatedRateProvider(childNames[i], serviceProvider.GetRequiredKeyedService<IDatedRateProvider>(childNames[i]));

            RateAggregationOptions options = new();
            if (defaultStrategy is not null)
                options.DefaultStrategy = defaultStrategy;

            foreach ((CurrencyPair pair, string[]? order, IRateAggregationStrategy? strategy) in routes)
                options.Routes[pair] = new CurrencyPairRoute(order, strategy);

            return new AggregatingRateProvider(
                children,
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<AggregatingRateProvider>());
        });

        services.AddSingleton<IDatedRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingRateProvider>());
        services.AddSingleton<IRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingRateProvider>());

        return builder;
    }

    /// <summary>
    /// Adds and binds the shared <see cref="CachingRateOptions" /> from configuration and a callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The optional configuration root or section.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="configure">The optional post-binding callback.</param>
    private static void BindCacheOptions(
        IServiceCollection services,
        IConfiguration? configuration,
        string sectionName,
        Action<CachingRateOptions>? configure)
    {
        OptionsBuilder<CachingRateOptions> optionsBuilder = services.AddOptions<CachingRateOptions>();

        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));

        if (configure is not null)
            optionsBuilder.Configure(configure);

        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Caching exchange-rate options are invalid.")
            .ValidateOnStart();
    }

    /// <summary>
    /// Builds a caching provider that wraps <paramref name="inner" /> under <paramref name="name" /> over the cache the
    /// <paramref name="cacheFactory" /> produces (or a default file cache), using the shared cache options and ambient
    /// time provider and logger.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies from.</param>
    /// <param name="name">The name the source is cached under.</param>
    /// <param name="inner">The source provider to wrap.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the cache; when <see langword="null" />, a default file cache is built.
    /// </param>
    /// <returns>A new <see cref="CachingRateProvider" />.</returns>
    private static CachingRateProvider CreateCachingProvider(
        IServiceProvider serviceProvider,
        string name,
        IDatedRateProvider inner,
        Func<IServiceProvider, string, IRateCache>? cacheFactory)
    {
        CachingRateOptions options = serviceProvider.GetRequiredService<IOptions<CachingRateOptions>>().Value;
        IRateCache cache = cacheFactory?.Invoke(serviceProvider, name) ?? CreateDefaultCache(serviceProvider, name, options);

        return new CachingRateProvider(
            inner,
            cache,
            options,
            serviceProvider.GetService<TimeProvider>(),
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CachingRateProvider>());
    }

    /// <summary>
    /// Builds the default cache for a named source: a <see cref="TomlFileRateCache" /> bound to
    /// <paramref name="name" /> under the options' cache directory.
    /// </summary>
    /// <param name="serviceProvider">
    /// The container the optional <see cref="TimeProvider" /> and logger are resolved from.
    /// </param>
    /// <param name="name">The provider name the cache is bound to.</param>
    /// <param name="options">The shared cache options carrying the cache directory.</param>
    /// <returns>A new default <see cref="TomlFileRateCache" />.</returns>
    private static TomlFileRateCache CreateDefaultCache(IServiceProvider serviceProvider, string name, CachingRateOptions options) =>
        new(
            new FileRateCacheOptions
            {
                Provider = name,
                CacheDirectory = options.CacheDirectory,
            },
            serviceProvider.GetService<TimeProvider>(),
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<TomlFileRateCache>());
}

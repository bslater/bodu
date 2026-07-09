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
    /// <summary>The default configuration section bound into <see cref="CachingExchangeRateOptions" />.</summary>
    private const string DefaultCacheSection = "Financial:ExchangeRateCache";

    /// <summary>
    /// Registers a <see cref="CachingExchangeRateProvider" /> that wraps a single source
    /// <typeparamref name="TProvider" /> over its own on-disk cache, resolvable as both
    /// <see cref="IDatedRateProvider" /> and the timeless <see cref="IRateProvider" />.
    /// </summary>
    /// <typeparam name="TProvider">The concrete source provider to cache.</typeparam>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="providerName">The name the source's rates are cached under.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="CachingExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:ExchangeRateCache</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the <see cref="IExchangeRateCache" /> from the service provider and the provider
    /// name. When <see langword="null" />, a default <see cref="TomlFileExchangeRateCache" /> bound to
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
    /// package's registration such as <c>AddRbaHistoricalRates</c>. This method resolves the registered instance and
    /// wraps it in a caching decorator; it does not construct the source or its own dependencies (such as its
    /// <see cref="HttpClient" />), so registering only the cache without the source fails when the provider is
    /// resolved.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddFinancialService()
    ///         .AddRbaHistoricalRates(configuration)
    ///         .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA", configuration,
    ///             configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
    ///
    /// // Consumers resolve IDatedRateProvider (or IRateProvider) and get cached lookups transparently.
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddCachedExchangeRateProvider<TProvider>(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<CachingExchangeRateOptions>? configure = null,
        Func<IServiceProvider, string, IExchangeRateCache>? cacheFactory = null)
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
        services.AddSingleton<IDatedRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingExchangeRateProvider>());
        services.AddSingleton<IRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingExchangeRateProvider>());

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="AggregatingExchangeRateProvider" /> that groups the cached children added through
    /// <paramref name="configure" />, resolvable as both <see cref="IDatedRateProvider" /> and the timeless
    /// <see cref="IRateProvider" />. Each child is also registered as a keyed
    /// <see cref="IDatedRateProvider" /> so a specific source can be resolved by name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configure">A callback that adds the cached children and configures routing and strategy.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into the shared <see cref="CachingExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:ExchangeRateCache</c>.
    /// </param>
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
    ///         .AddAggregatedExchangeRateProvider(agg => agg
    ///             .AddCachedChild<RbaExchangeRateProvider>("RBA")
    ///             .AddCachedChild<EcbExchangeRateProvider>("ECB")
    ///             .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
    ///             .MapPair(new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP), "ECB", "RBA"));
    ///
    /// // Resolve the aggregate, or a specific source by name:
    /// var aggregate = provider.GetRequiredService<IDatedRateProvider>();
    /// var rbaOnly = provider.GetRequiredKeyedService<IDatedRateProvider>("RBA");
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddAggregatedExchangeRateProvider(
        this IFinancialServiceBuilder builder,
        Action<IAggregatedExchangeRateBuilder> configure,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<CachingExchangeRateOptions>? configureCache = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        AggregatedExchangeRateBuilder aggregateBuilder = new();
        configure(aggregateBuilder);

        IServiceCollection services = builder.Services;
        BindCacheOptions(services, configuration, sectionName, configureCache);

        // Register each child keyed by name so a specific cached source is resolvable through the service catalog.
        foreach ((string Name, Func<IServiceProvider, IDatedRateProvider> Factory, Func<IServiceProvider, string, IExchangeRateCache>? CacheFactory) child in aggregateBuilder.Children)
        {
            Func<IServiceProvider, IDatedRateProvider> factory = child.Factory;
            Func<IServiceProvider, string, IExchangeRateCache>? cacheFactory = child.CacheFactory;
            services.TryAddKeyedSingleton<IDatedRateProvider>(
                child.Name,
                (serviceProvider, key) => CreateCachingProvider(serviceProvider, (string)key!, factory(serviceProvider), cacheFactory));
        }

        string[] childNames = new string[aggregateBuilder.Children.Count];
        for (int i = 0; i < childNames.Length; i++)
            childNames[i] = aggregateBuilder.Children[i].Name;

        IReadOnlyList<(CurrencyPair Pair, string[] ProviderOrder, IExchangeRateAggregationStrategy? Strategy)> routes = aggregateBuilder.Routes;
        IExchangeRateAggregationStrategy? defaultStrategy = aggregateBuilder.DefaultStrategy;

        services.AddSingleton(serviceProvider =>
        {
            var children = new NamedDatedExchangeRateProvider[childNames.Length];
            for (int i = 0; i < childNames.Length; i++)
                children[i] = new NamedDatedExchangeRateProvider(childNames[i], serviceProvider.GetRequiredKeyedService<IDatedRateProvider>(childNames[i]));

            ExchangeRateAggregationOptions options = new();
            if (defaultStrategy is not null)
                options.DefaultStrategy = defaultStrategy;

            foreach ((CurrencyPair pair, string[]? order, IExchangeRateAggregationStrategy? strategy) in routes)
                options.Routes[pair] = new CurrencyPairRoute(order, strategy);

            return new AggregatingExchangeRateProvider(
                children,
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<AggregatingExchangeRateProvider>());
        });

        services.AddSingleton<IDatedRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingExchangeRateProvider>());
        services.AddSingleton<IRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingExchangeRateProvider>());

        return builder;
    }

    /// <summary>
    /// Adds and binds the shared <see cref="CachingExchangeRateOptions" /> from configuration and a callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The optional configuration root or section.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="configure">The optional post-binding callback.</param>
    private static void BindCacheOptions(
        IServiceCollection services,
        IConfiguration? configuration,
        string sectionName,
        Action<CachingExchangeRateOptions>? configure)
    {
        OptionsBuilder<CachingExchangeRateOptions> optionsBuilder = services.AddOptions<CachingExchangeRateOptions>();

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
    /// <returns>A new <see cref="CachingExchangeRateProvider" />.</returns>
    private static CachingExchangeRateProvider CreateCachingProvider(
        IServiceProvider serviceProvider,
        string name,
        IDatedRateProvider inner,
        Func<IServiceProvider, string, IExchangeRateCache>? cacheFactory)
    {
        CachingExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<CachingExchangeRateOptions>>().Value;
        IExchangeRateCache cache = cacheFactory?.Invoke(serviceProvider, name) ?? CreateDefaultCache(name, options);

        return new CachingExchangeRateProvider(
            inner,
            cache,
            options,
            serviceProvider.GetService<TimeProvider>(),
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CachingExchangeRateProvider>());
    }

    /// <summary>
    /// Builds the default cache for a named source: a <see cref="TomlFileExchangeRateCache" /> bound to
    /// <paramref name="name" /> under the options' cache directory.
    /// </summary>
    /// <param name="name">The provider name the cache is bound to.</param>
    /// <param name="options">The shared cache options carrying the cache directory.</param>
    /// <returns>A new default <see cref="IExchangeRateCache" />.</returns>
    private static IExchangeRateCache CreateDefaultCache(string name, CachingExchangeRateOptions options) =>
        new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
        {
            Provider = name,
            CacheDirectory = options.CacheDirectory,
        });
}

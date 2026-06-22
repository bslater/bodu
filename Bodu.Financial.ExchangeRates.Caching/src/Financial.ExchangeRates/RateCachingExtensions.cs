// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Financial;
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
    /// <see cref="IDatedExchangeRateProvider" /> and the timeless <see cref="IExchangeRateProvider" />.
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
    /// // Consumers resolve IDatedExchangeRateProvider (or IExchangeRateProvider) and get cached lookups transparently.
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddCachedExchangeRateProvider<TProvider>(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<CachingExchangeRateOptions>? configure = null)
        where TProvider : class, IDatedExchangeRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;
        BindCacheOptions(services, configuration, sectionName, configure);

        services.TryAddSingleton<TProvider>();
        services.AddSingleton(serviceProvider =>
            CreateCachingProvider(serviceProvider, providerName, serviceProvider.GetRequiredService<TProvider>()));
        services.AddSingleton<IDatedExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingExchangeRateProvider>());
        services.AddSingleton<IExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<CachingExchangeRateProvider>());

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="AggregatingExchangeRateProvider" /> that groups the cached children added through
    /// <paramref name="configure" />, resolvable as both <see cref="IDatedExchangeRateProvider" /> and the timeless
    /// <see cref="IExchangeRateProvider" />. Each child is also registered as a keyed
    /// <see cref="IDatedExchangeRateProvider" /> so a specific source can be resolved by name.
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
    ///             .MapPair(new ExchangeRatePair("AUD", "USD"), "RBA", "ECB")
    ///             .MapPair(new ExchangeRatePair("USD", "GBP"), "ECB", "RBA"));
    ///
    /// // Resolve the aggregate, or a specific source by name:
    /// var aggregate = provider.GetRequiredService<IDatedExchangeRateProvider>();
    /// var rbaOnly = provider.GetRequiredKeyedService<IDatedExchangeRateProvider>("RBA");
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
        foreach (KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>> child in aggregateBuilder.Children)
        {
            Func<IServiceProvider, IDatedExchangeRateProvider> factory = child.Value;
            services.TryAddKeyedSingleton<IDatedExchangeRateProvider>(
                child.Key,
                (serviceProvider, key) => CreateCachingProvider(serviceProvider, (string)key!, factory(serviceProvider)));
        }

        string[] childNames = new string[aggregateBuilder.Children.Count];
        for (int i = 0; i < childNames.Length; i++)
            childNames[i] = aggregateBuilder.Children[i].Key;

        IReadOnlyList<(ExchangeRatePair Pair, string[] ProviderOrder, IExchangeRateAggregationStrategy? Strategy)> routes = aggregateBuilder.Routes;
        IExchangeRateAggregationStrategy? defaultStrategy = aggregateBuilder.DefaultStrategy;

        services.AddSingleton(serviceProvider =>
        {
            var children = new NamedDatedExchangeRateProvider[childNames.Length];
            for (int i = 0; i < childNames.Length; i++)
                children[i] = new NamedDatedExchangeRateProvider(childNames[i], serviceProvider.GetRequiredKeyedService<IDatedExchangeRateProvider>(childNames[i]));

            ExchangeRateAggregationOptions options = new();
            if (defaultStrategy is not null)
                options.DefaultStrategy = defaultStrategy;

            foreach ((ExchangeRatePair pair, string[]? order, IExchangeRateAggregationStrategy? strategy) in routes)
                options.Routes[pair] = new ExchangeRatePairRoute(order, strategy);

            return new AggregatingExchangeRateProvider(
                children,
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<AggregatingExchangeRateProvider>());
        });

        services.AddSingleton<IDatedExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingExchangeRateProvider>());
        services.AddSingleton<IExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<AggregatingExchangeRateProvider>());

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
    /// Builds a caching provider that wraps <paramref name="inner" /> under <paramref name="name" /> using the shared
    /// cache options and ambient time provider and logger.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies from.</param>
    /// <param name="name">The name the source is cached under.</param>
    /// <param name="inner">The source provider to wrap.</param>
    /// <returns>A new <see cref="CachingExchangeRateProvider" />.</returns>
    private static CachingExchangeRateProvider CreateCachingProvider(IServiceProvider serviceProvider, string name, IDatedExchangeRateProvider inner) =>
        new(
            name,
            inner,
            serviceProvider.GetRequiredService<IOptions<CachingExchangeRateOptions>>().Value,
            serviceProvider.GetService<TimeProvider>(),
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CachingExchangeRateProvider>());
}

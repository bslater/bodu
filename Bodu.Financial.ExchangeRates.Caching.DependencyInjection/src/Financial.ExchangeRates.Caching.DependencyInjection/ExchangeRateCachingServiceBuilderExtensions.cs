// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachingServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

/// <summary>
/// Provides the fluent registration of the caching exchange-rate provider onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class ExchangeRateCachingServiceBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="CachingDatedExchangeRateProvider" /> as the <see cref="IDatedExchangeRateProvider" />,
    /// wrapping the named sources added through the fluent <paramref name="configureSources" /> builder over a shared
    /// on-disk cache.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configureSources">
    /// A callback that adds the named sources to wrap, in priority order — for example
    /// <c>s =&gt; s.AddSource&lt;YahooExchangeRateProvider&gt;(YahooExchangeRateProvider.ProviderName)</c>.
    /// </param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="CachingExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:ExchangeRateCache</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="configureSources" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddBoduFinancial()
    ///         .AddYahooExchangeRates()
    ///         .AddRbaHistoricalRates()
    ///         .AddCachedExchangeRateProvider(
    ///             sources => sources
    ///                 .AddSource<YahooExchangeRateProvider>(YahooExchangeRateProvider.ProviderName)
    ///                 .AddSource<RbaExchangeRateProvider>(RbaExchangeRateProvider.ProviderName),
    ///             configure: o =>
    ///             {
    ///                 o.CacheDirectory = "/var/cache/fx";
    ///                 o.DefaultExpiry = TimeSpan.FromHours(12);
    ///                 o.ProviderExpiry[RbaExchangeRateProvider.ProviderName] = TimeSpan.FromDays(7);
    ///             });
    ///
    /// // Consumers resolve IDatedExchangeRateProvider and transparently get cached single-date and range lookups.
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddCachedExchangeRateProvider(
        this IFinancialServiceBuilder builder,
        Action<ICachedExchangeRateSourceBuilder> configureSources,
        IConfiguration? configuration = null,
        string sectionName = "Financial:ExchangeRateCache",
        Action<CachingExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureSources);

        CachedExchangeRateSourceBuilder sourceBuilder = new();
        configureSources(sourceBuilder);
        IReadOnlyList<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> sources = sourceBuilder.Sources;

        return builder.AddCachedExchangeRateProvider(
            serviceProvider => Materialize(sources, serviceProvider),
            configuration,
            sectionName,
            configure);
    }

    /// <summary>
    /// Registers a <see cref="CachingDatedExchangeRateProvider" /> as the <see cref="IDatedExchangeRateProvider" />,
    /// wrapping the named sources resolved by <paramref name="sources" /> over a shared on-disk cache.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="sources">
    /// A factory that resolves the named sources to wrap, in priority order, from the service provider.
    /// </param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="CachingExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:ExchangeRateCache</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="sources" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The named sources are typically the concrete providers registered by their own packages, for example
    /// <c>sp =&gt; new[] { new KeyValuePair&lt;string, IDatedExchangeRateProvider&gt;(YahooExchangeRateProvider.ProviderName, sp.GetRequiredService&lt;YahooExchangeRateProvider&gt;()) }</c>
    /// . Only the dated <see cref="IDatedExchangeRateProvider" /> surface is registered through the cache; any undated
    /// <see cref="IExchangeRateProvider" /> registration continues to resolve its concrete provider.
    /// </remarks>
    public static IFinancialServiceBuilder AddCachedExchangeRateProvider(
        this IFinancialServiceBuilder builder,
        Func<IServiceProvider, IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>>> sources,
        IConfiguration? configuration = null,
        string sectionName = "Financial:ExchangeRateCache",
        Action<CachingExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(sources);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<CachingExchangeRateOptions> optionsBuilder = services.AddOptions<CachingExchangeRateOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);

        services.TryAddSingleton<IExchangeRateCache>(static serviceProvider =>
            new TomlFileSystemExchangeRateCache(new FileSystemExchangeRateCacheOptions
            {
                CacheDirectory = serviceProvider.GetRequiredService<IOptions<CachingExchangeRateOptions>>().Value.CacheDirectory,
            }));

        services.AddSingleton<IDatedExchangeRateProvider>(serviceProvider =>
            new CachingDatedExchangeRateProvider(
                sources(serviceProvider),
                serviceProvider.GetRequiredService<IExchangeRateCache>(),
                serviceProvider.GetRequiredService<IOptions<CachingExchangeRateOptions>>().Value,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CachingDatedExchangeRateProvider>()));

        return builder;
    }

    /// <summary>
    /// Resolves each named source factory against the service provider, preserving order.
    /// </summary>
    /// <param name="sources">The named source factories.</param>
    /// <param name="serviceProvider">The service provider to resolve from.</param>
    /// <returns>The resolved named sources.</returns>
    private static IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>> Materialize(
        IReadOnlyList<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> sources,
        IServiceProvider serviceProvider)
    {
        KeyValuePair<string, IDatedExchangeRateProvider>[] resolved = new KeyValuePair<string, IDatedExchangeRateProvider>[sources.Count];
        for (var i = 0; i < sources.Count; i++)
            resolved[i] = new KeyValuePair<string, IDatedExchangeRateProvider>(sources[i].Key, sources[i].Value(serviceProvider));

        return resolved;
    }
}


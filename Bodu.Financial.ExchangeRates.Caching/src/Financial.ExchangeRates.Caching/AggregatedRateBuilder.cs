// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatedRateBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The default <see cref="IAggregatedRateBuilder" />, accumulating named children, the default strategy, and
/// per-pair routes in the order they are configured.
/// </summary>
internal sealed class AggregatedRateBuilder
    : IAggregatedRateBuilder
{
    /// <summary>The accumulated named children, in insertion order: a source factory and an optional cache factory each.</summary>
    private readonly List<(string Name, Func<IServiceProvider, IDatedRateProvider> Factory, Func<IServiceProvider, string, IRateCache>? CacheFactory)> _children = new();

    /// <summary>The accumulated per-pair routes, in insertion order.</summary>
    private readonly List<(CurrencyPair Pair, string[] ProviderOrder, IRateAggregationStrategy? Strategy)> _routes = new();

    /// <summary>The configured default strategy, or <see langword="null" /> to use the aggregator's own default.</summary>
    private IRateAggregationStrategy? _defaultStrategy;

    /// <summary>
    /// Gets the accumulated named children, in insertion order.
    /// </summary>
    /// <value>The named children, each carrying a source factory and an optional per-child cache factory.</value>
    public IReadOnlyList<(string Name, Func<IServiceProvider, IDatedRateProvider> Factory, Func<IServiceProvider, string, IRateCache>? CacheFactory)> Children => _children;

    /// <summary>
    /// Gets the accumulated per-pair routes, in insertion order.
    /// </summary>
    /// <returns>The per-pair routes.</returns>
    public IReadOnlyList<(CurrencyPair Pair, string[] ProviderOrder, IRateAggregationStrategy? Strategy)> Routes => _routes;

    /// <summary>
    /// Gets the configured default strategy.
    /// </summary>
    /// <value>The default strategy, or <see langword="null" /> when the aggregator default applies.</value>
    public IRateAggregationStrategy? DefaultStrategy => _defaultStrategy;

    /// <inheritdoc />
    public IAggregatedRateBuilder AddCachedChild(
        string name,
        Func<IServiceProvider, IDatedRateProvider> factory,
        Func<IServiceProvider, string, IRateCache>? cacheFactory = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        ThrowHelper.ThrowIfNull(factory);

        _children.Add((name, factory, cacheFactory));
        return this;
    }

    /// <inheritdoc />
    public IAggregatedRateBuilder AddCachedChild<TProvider>(
        string name,
        Func<IServiceProvider, string, IRateCache>? cacheFactory = null)
        where TProvider : class, IDatedRateProvider =>
        AddCachedChild(name, static serviceProvider => serviceProvider.GetRequiredService<TProvider>(), cacheFactory);

    /// <inheritdoc />
    public IAggregatedRateBuilder UseDefaultStrategy(IRateAggregationStrategy strategy)
    {
        ThrowHelper.ThrowIfNull(strategy);

        _defaultStrategy = strategy;
        return this;
    }

    /// <inheritdoc />
    public IAggregatedRateBuilder MapPair(CurrencyPair pair, params string[] providerOrder)
    {
        ThrowHelper.ThrowIfNull(providerOrder);

        _routes.Add((pair, providerOrder, null));
        return this;
    }

    /// <inheritdoc />
    public IAggregatedRateBuilder MapPair(CurrencyPair pair, IRateAggregationStrategy strategy, params string[] providerOrder)
    {
        ThrowHelper.ThrowIfNull(strategy);
        ThrowHelper.ThrowIfNull(providerOrder);

        _routes.Add((pair, providerOrder, strategy));
        return this;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatedExchangeRateBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The default <see cref="IAggregatedExchangeRateBuilder" />, accumulating named children, the default strategy, and
/// per-pair routes in the order they are configured.
/// </summary>
internal sealed class AggregatedExchangeRateBuilder
    : IAggregatedExchangeRateBuilder
{
    /// <summary>The accumulated named child factories, in insertion order.</summary>
    private readonly List<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> _children = new();

    /// <summary>The accumulated per-pair routes, in insertion order.</summary>
    private readonly List<(ExchangeRatePair Pair, string[] ProviderOrder, IExchangeRateAggregationStrategy? Strategy)> _routes = new();

    /// <summary>The configured default strategy, or <see langword="null" /> to use the aggregator's own default.</summary>
    private IExchangeRateAggregationStrategy? _defaultStrategy;

    /// <summary>
    /// Gets the accumulated named child factories, in insertion order.
    /// </summary>
    /// <returns>The named child factories.</returns>
    public IReadOnlyList<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> Children => _children;

    /// <summary>
    /// Gets the accumulated per-pair routes, in insertion order.
    /// </summary>
    /// <returns>The per-pair routes.</returns>
    public IReadOnlyList<(ExchangeRatePair Pair, string[] ProviderOrder, IExchangeRateAggregationStrategy? Strategy)> Routes => _routes;

    /// <summary>
    /// Gets the configured default strategy.
    /// </summary>
    /// <returns>The default strategy, or <see langword="null" /> when the aggregator default applies.</returns>
    public IExchangeRateAggregationStrategy? DefaultStrategy => _defaultStrategy;

    /// <inheritdoc />
    public IAggregatedExchangeRateBuilder AddCachedChild(string name, Func<IServiceProvider, IDatedExchangeRateProvider> factory)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        ThrowHelper.ThrowIfNull(factory);

        _children.Add(new KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>(name, factory));
        return this;
    }

    /// <inheritdoc />
    public IAggregatedExchangeRateBuilder AddCachedChild<TProvider>(string name)
        where TProvider : class, IDatedExchangeRateProvider =>
        AddCachedChild(name, static serviceProvider => serviceProvider.GetRequiredService<TProvider>());

    /// <inheritdoc />
    public IAggregatedExchangeRateBuilder UseDefaultStrategy(IExchangeRateAggregationStrategy strategy)
    {
        ThrowHelper.ThrowIfNull(strategy);

        _defaultStrategy = strategy;
        return this;
    }

    /// <inheritdoc />
    public IAggregatedExchangeRateBuilder MapPair(ExchangeRatePair pair, params string[] providerOrder)
    {
        ThrowHelper.ThrowIfNull(providerOrder);

        _routes.Add((pair, providerOrder, null));
        return this;
    }

    /// <inheritdoc />
    public IAggregatedExchangeRateBuilder MapPair(ExchangeRatePair pair, IExchangeRateAggregationStrategy strategy, params string[] providerOrder)
    {
        ThrowHelper.ThrowIfNull(strategy);
        ThrowHelper.ThrowIfNull(providerOrder);

        _routes.Add((pair, providerOrder, strategy));
        return this;
    }
}

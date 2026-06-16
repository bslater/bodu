// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairRoute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Describes how an <see cref="AggregatingExchangeRateProvider" /> resolves a specific currency pair: the ordered child
/// provider names to consult and, optionally, the strategy to combine them.
/// </summary>
/// <remarks>
/// A route lets a single aggregator prefer different sources for different pairs — for example <c>AUD/USD</c> via
/// <c>[RBA, ECB]</c> while <c>USD/GBP</c> prefers <c>[ECB, RBA]</c> — and optionally override the aggregator's default
/// strategy for that pair.
/// </remarks>
public sealed class ExchangeRatePairRoute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePairRoute" /> class.
    /// </summary>
    /// <param name="providerOrder">The ordered child provider names to consult for the routed pair.</param>
    /// <param name="strategy">
    /// The strategy used to combine the routed providers, or <see langword="null" /> to use the aggregator's default
    /// strategy.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerOrder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerOrder" /> is empty or contains a blank name.
    /// </exception>
    public ExchangeRatePairRoute(IEnumerable<string> providerOrder, IExchangeRateAggregationStrategy? strategy = null)
    {
        ThrowHelper.ThrowIfNull(providerOrder);

        string[] order = [.. providerOrder];
        if (order.Length == 0)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_AggregationRouteEmpty, nameof(providerOrder));

        for (int i = 0; i < order.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(order[i]))
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNameBlank, nameof(providerOrder));
        }

        ProviderOrder = order;
        Strategy = strategy;
    }

    /// <summary>
    /// Gets the ordered child provider names consulted for the routed pair.
    /// </summary>
    /// <returns>The ordered provider names.</returns>
    public IReadOnlyList<string> ProviderOrder { get; }

    /// <summary>
    /// Gets the strategy used to combine the routed providers, if overridden.
    /// </summary>
    /// <returns>
    /// The route-specific strategy, or <see langword="null" /> to use the aggregator's default strategy.
    /// </returns>
    public IExchangeRateAggregationStrategy? Strategy { get; }
}

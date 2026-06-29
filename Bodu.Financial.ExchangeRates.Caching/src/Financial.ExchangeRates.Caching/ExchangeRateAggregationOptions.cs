// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateAggregationOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures an <see cref="AggregatingExchangeRateProvider" />: the default combination strategy, the default child
/// order, and per-currency-pair routing overrides.
/// </summary>
/// <remarks>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c>.
/// Referenced child names are validated against the aggregator's actual children when the aggregator is constructed.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new ExchangeRateAggregationOptions();
/// options.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
/// options.Routes[new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
/// options.Routes[new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());
///]]>
/// </code>
/// </example>
public sealed class ExchangeRateAggregationOptions
{
    /// <summary>
    /// Gets or sets the strategy used when a pair has no route-specific strategy.
    /// </summary>
    /// <value>The default strategy; defaults to <see cref="PriorityFallbackStrategy.Instance" />.</value>
    public IExchangeRateAggregationStrategy DefaultStrategy { get; set; } = PriorityFallbackStrategy.Instance;

    /// <summary>
    /// Gets or sets the default order of child names consulted for pairs without a route.
    /// </summary>
    /// <value>
    /// The ordered child names, or <see langword="null" /> to consult every child in the order supplied to the
    /// aggregator.
    /// </value>
    public IReadOnlyList<string>? DefaultProviderOrder { get; set; }

    /// <summary>
    /// Gets the per-currency-pair routing overrides.
    /// </summary>
    /// <value>A map from currency pair to the route that resolves it.</value>
    public IDictionary<ExchangeRatePair, ExchangeRatePairRoute> Routes { get; } = new Dictionary<ExchangeRatePair, ExchangeRatePairRoute>();

    /// <summary>
    /// Gets or sets the lookup options applied by the timeless
    /// <see cref="IExchangeRateProvider.GetRate(string, string)" /> surface, which resolves the rate for the current
    /// UTC date.
    /// </summary>
    /// <value>
    /// The lookup options used for timeless lookups; defaults to <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </value>
    public ExchangeRateLookupOptions DefaultLookupOptions { get; set; } = ExchangeRateLookupOptions.Exact;

    /// <summary>
    /// Gets or sets the level at which the selected route is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel RouteSelectedLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the level at which a successfully aggregated lookup is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel ResolvedLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the level at which a lookup that no candidate could satisfy is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel UnresolvedLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Validates the structural option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="DefaultStrategy" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a route is keyed by an invalid currency pair, a route value is <see langword="null" />, or a name in
    /// <see cref="DefaultProviderOrder" /> is blank.
    /// </exception>
    public void Validate()
    {
        ThrowHelper.ThrowIfNull(DefaultStrategy);

        foreach (KeyValuePair<ExchangeRatePair, ExchangeRatePairRoute> entry in Routes)
        {
            if (!entry.Key.IsValid)
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_AggregationRoutePair, nameof(Routes));

            if (entry.Value is null)
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_AggregationRouteNull, nameof(Routes));
        }

        if (DefaultProviderOrder is not null)
        {
            foreach (string name in DefaultProviderOrder)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNameBlank, nameof(DefaultProviderOrder));
            }
        }
    }
}

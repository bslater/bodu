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
/// options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" });
/// options.Routes[new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP)] = new CurrencyPairRoute(new[] { "ECB", "RBA" });
/// options.Routes[new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());
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
    public IDictionary<CurrencyPair, CurrencyPairRoute> Routes { get; } = new Dictionary<CurrencyPair, CurrencyPairRoute>();

    /// <summary>
    /// Gets or sets a value indicating whether the aggregator consults each child's advertised
    /// <see cref="RateHistoryAvailability" /> and drops candidates that have declared they cannot serve any
    /// part of the requested date or window, before the strategy runs.
    /// </summary>
    /// <value><see langword="true" /> to respect the advertised history; defaults to <see langword="true" />.</value>
    /// <remarks>
    /// The filter applies only to children that implement <see cref="IHistoryAwareRateProvider" />; a non-aware
    /// child is treated as unbounded and always kept. A range keeps any child whose advertised history overlaps the
    /// window at all, since strategies already tolerate partial data. When every candidate is filtered out, the lookup
    /// reports the same miss it reports when every candidate fails. Disable this to offer every routed candidate to the
    /// strategy unchanged.
    /// </remarks>
    public bool RespectHistoryAvailability { get; set; } = true;

    /// <summary>
    /// Gets or sets the lookup options applied by the timeless
    /// <see cref="IRateProvider.GetRate(string, string)" /> surface, which resolves the rate for the current
    /// UTC date.
    /// </summary>
    /// <value>
    /// The lookup options used for timeless lookups; defaults to <see cref="RateLookupOptions.Exact" />.
    /// </value>
    public RateLookupOptions DefaultLookupOptions { get; set; } = RateLookupOptions.Exact;

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

        foreach (KeyValuePair<CurrencyPair, CurrencyPairRoute> entry in Routes)
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

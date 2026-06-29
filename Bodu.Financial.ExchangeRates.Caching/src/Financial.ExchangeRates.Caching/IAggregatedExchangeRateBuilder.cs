// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAggregatedExchangeRateBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Builds the set of named, individually cached children an <see cref="AggregatingExchangeRateProvider" /> groups,
/// together with its default strategy and per-currency-pair routing.
/// </summary>
/// <remarks>
/// Each child is registered as a keyed <see cref="IDatedExchangeRateProvider" /> (and resolvable by name through the
/// service catalog) wrapped in its own <see cref="CachingExchangeRateProvider" />, so a specific source can be obtained
/// directly while the aggregator applies the configured strategy and routing.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// services.AddFinancialService()
///         .AddAggregatedExchangeRateProvider(agg => agg
///             .AddCachedChild<RbaExchangeRateProvider>("RBA")
///             .AddCachedChild<EcbExchangeRateProvider>("ECB")
///             .UseDefaultStrategy(new PriorityFallbackStrategy())
///             .MapPair(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
///             .MapPair(new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD), new AverageStrategy(), "ECB", "RBA"));
///]]>
/// </code>
/// </example>
/// </remarks>
public interface IAggregatedExchangeRateBuilder
{
    /// <summary>
    /// Adds a cached child resolved from the container by its concrete type.
    /// </summary>
    /// <typeparam name="TProvider">The concrete provider type to resolve and cache.</typeparam>
    /// <param name="name">The name the child is referenced and cached under.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the child's <see cref="IExchangeRateCache" /> from the service provider and the
    /// child name. When <see langword="null" />, the registration's default cache is used.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or white space.</exception>
    IAggregatedExchangeRateBuilder AddCachedChild<TProvider>(
        string name,
        Func<IServiceProvider, string, IExchangeRateCache>? cacheFactory = null)
        where TProvider : class, IDatedExchangeRateProvider;

    /// <summary>
    /// Adds a cached child resolved by a factory.
    /// </summary>
    /// <param name="name">The name the child is referenced and cached under.</param>
    /// <param name="factory">A factory that resolves the child source from the service provider.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the child's <see cref="IExchangeRateCache" /> from the service provider and the
    /// child name. When <see langword="null" />, the registration's default cache is used.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or white space.</exception>
    IAggregatedExchangeRateBuilder AddCachedChild(
        string name,
        Func<IServiceProvider, IDatedExchangeRateProvider> factory,
        Func<IServiceProvider, string, IExchangeRateCache>? cacheFactory = null);

    /// <summary>
    /// Sets the strategy applied to pairs without a route-specific strategy.
    /// </summary>
    /// <param name="strategy">The default aggregation strategy.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="strategy" /> is <see langword="null" />.
    /// </exception>
    IAggregatedExchangeRateBuilder UseDefaultStrategy(IExchangeRateAggregationStrategy strategy);

    /// <summary>
    /// Routes a currency pair to an ordered set of child names, combined with the default strategy.
    /// </summary>
    /// <param name="pair">The currency pair to route.</param>
    /// <param name="providerOrder">The ordered child names to consult for the pair.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerOrder" /> is <see langword="null" />.
    /// </exception>
    IAggregatedExchangeRateBuilder MapPair(ExchangeRatePair pair, params string[] providerOrder);

    /// <summary>
    /// Routes a currency pair to an ordered set of child names, combined with a pair-specific strategy.
    /// </summary>
    /// <param name="pair">The currency pair to route.</param>
    /// <param name="strategy">The strategy used to combine the routed children.</param>
    /// <param name="providerOrder">The ordered child names to consult for the pair.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="strategy" /> or <paramref name="providerOrder" /> is <see langword="null" />.
    /// </exception>
    IAggregatedExchangeRateBuilder MapPair(ExchangeRatePair pair, IExchangeRateAggregationStrategy strategy, params string[] providerOrder);
}

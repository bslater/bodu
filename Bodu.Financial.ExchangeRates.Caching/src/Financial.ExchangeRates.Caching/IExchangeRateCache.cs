// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Persists fetched exchange rates for a single provider in a pair-keyed store so they need not be re-fetched while
/// fresh.
/// </summary>
/// <remarks>
/// <para>
/// A cache instance is bound to exactly one provider, exposed through <see cref="Provider" /> and fixed at construction
/// rather than supplied on each call. Group several providers by composing one cache (and one caching provider) per
/// provider behind an aggregating provider.
/// </para>
/// <para>
/// The cache owns expiry: callers supply the caching <c>duration</c> on each call, and the cache returns only fresh
/// rows from <see cref="GetRates" /> and prunes stale rows when it merges on <see cref="Store" />, so the backing store
/// self-cleans over time.
/// </para>
/// <para>
/// Implementations are expected to be resilient: a cache failure should manifest as an empty result or a no-op rather
/// than an exception that breaks rate retrieval.
/// </para>
/// </remarks>
public interface IExchangeRateCache
{
    /// <summary>
    /// Gets the name of the provider this cache stores rates for.
    /// </summary>
    /// <returns>The provider identifier the cache is bound to.</returns>
    string Provider { get; }

    /// <summary>
    /// Returns the fresh cached rates for the supplied pair, evaluated against <paramref name="asOf" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration a cached rate remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <returns>The fresh cached rates ordered by date, or an empty list when none are fresh or available.</returns>
    IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf);

    /// <summary>
    /// Stores rates for the supplied pair, merging with any existing entry so the most recently cached rate wins per
    /// date, and pruning rows that are no longer fresh under <paramref name="duration" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="rates">The rates to store.</param>
    /// <param name="duration">The duration a cached rate remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which stale rows are pruned.</param>
    void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf);
}

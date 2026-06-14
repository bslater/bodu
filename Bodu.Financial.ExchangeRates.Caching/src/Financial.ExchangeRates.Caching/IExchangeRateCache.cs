// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Persists fetched exchange rates in a provider- and pair-keyed store so they need not be re-fetched while fresh.
/// </summary>
/// <remarks>
/// <para>
/// The cache owns expiry: callers supply the caching <c>duration</c> on each call, and the cache returns only fresh
/// rows from <see cref="GetRates" /> and prunes stale rows when it merges on <see cref="Store" />, so files self-clean
/// over time.
/// </para>
/// <para>
/// Implementations are expected to be resilient: a cache failure should manifest as an empty result or a no-op rather
/// than an exception that breaks rate retrieval.
/// </para>
/// </remarks>
public interface IExchangeRateCache
{
    /// <summary>
    /// Returns the fresh cached rates for the supplied provider and pair, evaluated against <paramref name="asOf" />.
    /// </summary>
    /// <param name="provider">The provider identifier the rates were cached under.</param>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration a cached rate remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <returns>The fresh cached rates ordered by date, or an empty list when none are fresh or available.</returns>
    IReadOnlyList<CachedExchangeRate> GetRates(string provider, ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf);

    /// <summary>
    /// Stores rates for the supplied provider and pair, merging with any existing entry so the most recently cached
    /// rate wins per date, and pruning rows that are no longer fresh under <paramref name="duration" />.
    /// </summary>
    /// <param name="provider">The provider identifier to cache the rates under.</param>
    /// <param name="pair">The currency pair.</param>
    /// <param name="rates">The rates to store.</param>
    /// <param name="duration">The duration a cached rate remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which stale rows are pruned.</param>
    void Store(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf);
}

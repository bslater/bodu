// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IYahooRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Persists fetched exchange rates in a provider- and pair-keyed format so they need not be re-fetched while fresh.
/// </summary>
/// <remarks>
/// The cache is mechanism-only: it stores and returns <see cref="CachedExchangeRate" /> rows but applies no expiry
/// policy of its own. The provider decides freshness from each row's retrieval time. Implementations are expected to be
/// resilient: a cache failure should manifest as an empty result or a no-op rather than an exception that breaks rate
/// retrieval.
/// </remarks>
public interface IYahooRateCache
{
    /// <summary>
    /// Returns all cached rates for the supplied pair and provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The cached rates, or an empty list when none are cached.</returns>
    IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, string provider);

    /// <summary>
    /// Stores rates for the supplied pair and provider, merging with any existing entry so the most recently retrieved
    /// rate wins per date.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="rates">The rates to store.</param>
    void Store(ExchangeRatePair pair, string provider, IReadOnlyList<CachedExchangeRate> rates);
}

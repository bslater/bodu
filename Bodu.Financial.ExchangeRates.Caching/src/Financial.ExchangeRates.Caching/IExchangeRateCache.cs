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
/// Alongside rate rows, the cache persists <em>coverage</em>: the date ranges that were actually fetched, recorded via
/// <see cref="RecordCoverage" /> and read back as a <see cref="DateRangeCoverage" /> via <see cref="GetCoverage" />.
/// Coverage is what makes a range lookup correct — a sparse set of rows can span a window without every interior day
/// having been fetched, so a range is served from the cache only when its coverage contains the whole window.
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

    /// <summary>
    /// Returns the union of the still-fresh coverage windows recorded for the supplied pair, evaluated against
    /// <paramref name="asOf" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <param name="asOf">The instant against which coverage freshness is evaluated.</param>
    /// <returns>
    /// A <see cref="DateRangeCoverage" /> describing the days known to have been fetched and still fresh; empty when no
    /// fresh coverage exists.
    /// </returns>
    /// <remarks>
    /// Coverage answers which days were actually fetched, not merely which days have a cached rate. A range lookup
    /// should be served from the cache only when this coverage
    /// <see cref="DateRangeCoverage.Contains(DateOnly, DateOnly)" /> the whole requested window, so an interior day
    /// that was never fetched forces a refetch rather than being served from a sparse set of rows.
    /// </remarks>
    DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf);

    /// <summary>
    /// Records that the inclusive range <paramref name="start" />..<paramref name="end" /> was fetched for the supplied
    /// pair, stamping the window at <paramref name="asOf" /> and pruning windows that are no longer fresh under
    /// <paramref name="duration" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="start">The inclusive first date of the fetched range.</param>
    /// <param name="end">The inclusive last date of the fetched range.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <param name="asOf">
    /// The instant the fetched window is stamped with and against which stale windows are pruned.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf);
}

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
/// A fetched range writes both halves at once through <see cref="StoreFetchedRange" />, which merges the fetched rows
/// and records the covered window as one atomic unit. Splitting that into a separate <see cref="Store" /> and
/// <see cref="RecordCoverage" /> risks persisting coverage without its rows on a backend that silently swallows a write
/// failure, which would let a later range lookup report a false hit and return incomplete data as if complete. Treat
/// <see cref="StoreFetchedRange" /> as the primary range-fetch contract every backend must satisfy;
/// <see cref="Store" /> (the single-date miss path) and <see cref="RecordCoverage" /> are the lower-level halves,
/// intended for the cache's own composition rather than a provider fetch path.
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
    /// <value>The provider identifier the cache is bound to.</value>
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
    /// <remarks>
    /// This stores rate rows only and records no coverage window, so it is the path a single-date miss caches its
    /// resolved row through. Because no coverage is recorded, a later range lookup that spans those dates still
    /// refetches rather than serving from these rows — by design, since only a range fetch (through
    /// <see cref="StoreFetchedRange" />) establishes the contiguous coverage a range serve requires.
    /// </remarks>
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
    /// <remarks>
    /// This is a low-level building block that records the coverage half only, with no rows. It exists for the cache's
    /// own composition and is <strong>not</strong> the path a provider fetch should use: recording coverage here and
    /// writing rows through a separate <see cref="Store" /> risks persisting coverage a backend later serves as a false
    /// hit. A range fetch must instead use <see cref="StoreFetchedRange" />, which writes both halves atomically.
    /// </remarks>
    void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf);

    /// <summary>
    /// Atomically merges <paramref name="rows" /> into the pair's cached rows and records the inclusive range
    /// <paramref name="start" />..<paramref name="end" /> as covered, persisting both halves together or neither.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="rows">
    /// The rows fetched for the range, which may be empty when the fetch returned no observation.
    /// </param>
    /// <param name="start">The inclusive first date of the fetched range.</param>
    /// <param name="end">The inclusive last date of the fetched range.</param>
    /// <param name="duration">
    /// The duration a cached row and a recorded coverage window remain fresh after they were cached or fetched.
    /// </param>
    /// <param name="asOf">
    /// The instant newly cached rows and the fetched window are stamped with and against which stale rows and windows
    /// are pruned.
    /// </param>
    /// <returns>
    /// <see cref="ExchangeRateCacheWriteStatus.Stored" /> when both halves were persisted;
    /// <see cref="ExchangeRateCacheWriteStatus.Failed" /> when a storage error was swallowed and nothing was persisted;
    /// <see cref="ExchangeRateCacheWriteStatus.Skipped" /> for a cache that intentionally stores nothing.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rows" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The coverage window is recorded even when <paramref name="rows" /> is empty: a successful fetch that returned no
    /// observation (a weekend, a holiday, a true gap) must still mark the window covered so a later lookup of the same
    /// window is served rather than refetched.
    /// </para>
    /// <para>
    /// The merge follows the same most-recent-per-date rule as <see cref="Store" /> and the same window pruning as
    /// <see cref="RecordCoverage" />. The write is atomic per pair — under the per-pair lock for the in-memory and file
    /// caches, in one transaction for SQLite, and as one read-modify-write of the per-pair blob for a distributed cache
    /// — so a reader never observes coverage without its rows. As with the other write paths, a swallowed storage error
    /// degrades to <see cref="ExchangeRateCacheWriteStatus.Failed" /> rather than throwing; argument validation still
    /// throws.
    /// </para>
    /// </remarks>
    ExchangeRateCacheWriteStatus StoreFetchedRange(
        ExchangeRatePair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf);
}

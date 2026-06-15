// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic mechanism for an <see cref="IExchangeRateCache" />: read-time freshness filtering,
/// write-time merge-and-prune, and the recording and pruning of coverage windows for a single provider. Derived types
/// implement only the persistence of a <see cref="CachePairState" />; this base prescribes no physical storage
/// structure.
/// </summary>
/// <typeparam name="TOptions">The options type carrying the bound provider and any storage settings.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ReadState" /> returns the raw stored rows and coverage windows without filtering; this base applies the
/// freshness policy in <see cref="GetRates" /> and <see cref="GetCoverage" />, prunes stale rows in
/// <see cref="Store" />, and prunes stale coverage windows in <see cref="RecordCoverage" />, so the backing store
/// self-cleans on every write.
/// </para>
/// <para>
/// Rate rows and coverage windows are independent halves of the per-pair state: a write through one path preserves the
/// other half so that recording coverage never drops rows and storing rows never drops coverage. The read-modify-write
/// sequences in <see cref="Store" /> and <see cref="RecordCoverage" /> run under a per-pair lock so concurrent writes
/// to the same pair cannot interleave and lose either half. The lock is process-local; file caches remain best-effort
/// across processes, where the atomic temp-and-move write keeps each file internally consistent.
/// </para>
/// </remarks>
public abstract class ExchangeRateCacheBase<TOptions>
    : IExchangeRateCache
    where TOptions : ExchangeRateCacheOptions
{
    /// <summary>
    /// The clock-skew tolerance applied when validating a row's caching instant: a row stamped more than this far in
    /// the future of the evaluation instant is treated as invalid rather than fresh.
    /// </summary>
    private static readonly TimeSpan s_clockSkewTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The validated options carrying the bound provider and any storage settings.
    /// </summary>
    private readonly TOptions _options;

    /// <summary>
    /// The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" /> and
    /// <see cref="RecordCoverage" />. One lock object is created per pair on first use and reused thereafter.
    /// </summary>
    private readonly ConcurrentDictionary<ExchangeRatePair, object> _pairLocks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and any storage settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected ExchangeRateCacheBase(TOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _options = options;
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <summary>
    /// Gets the validated options the cache was constructed with.
    /// </summary>
    /// <returns>The cache options.</returns>
    protected TOptions Options => _options;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadState(pair).Entries;
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        List<CachedExchangeRate> fresh = new(entries.Count);
        foreach (CachedExchangeRate entry in entries)
        {
            if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                fresh.Add(entry);
        }

        fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return fresh;
    }

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            // Merge with any existing entry so the most recently cached rate wins per date.
            Dictionary<DateOnly, CachedExchangeRate> merged = new();
            foreach (CachedExchangeRate existing in state.Entries)
                merged[existing.Date] = existing;

            foreach (CachedExchangeRate rate in rates)
            {
                if (!IsValid(rate, asOf))
                    continue;

                if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.CachedAtUtc >= current.CachedAtUtc)
                    merged[rate.Date] = rate;
            }

            // Prune rows that are no longer fresh or are semantically invalid, then order by date so the store is stable
            // and self-cleaning.
            List<CachedExchangeRate> ordered = new(merged.Count);
            foreach (CachedExchangeRate entry in merged.Values)
            {
                if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                    ordered.Add(entry);
            }

            ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));

            // Preserve the existing coverage half: storing rows must never drop recorded coverage.
            WriteState(pair, new CachePairState(ordered, state.Coverage));
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        DateRangeCoverage coverage = new();
        foreach (CoverageWindow window in ReadState(pair).Coverage)
        {
            if (window.IsFresh(asOf, duration))
                coverage.Add(window.Start, window.End);
        }

        return coverage;
    }

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            // Keep the still-fresh windows, drop the rest, then append the newly fetched window so the store self-cleans.
            List<CoverageWindow> windows = new(state.Coverage.Count + 1);
            foreach (CoverageWindow window in state.Coverage)
            {
                if (window.IsFresh(asOf, duration))
                    windows.Add(window);
            }

            windows.Add(new CoverageWindow(start, end, asOf));

            // Preserve the existing entries half: recording coverage must never drop cached rows.
            WriteState(pair, new CachePairState(state.Entries, windows));
        }
    }

    /// <summary>
    /// Reads the raw, unfiltered persisted state for a pair: its cached rows together with its coverage windows.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>
    /// The stored state, or <see cref="CachePairState.Empty" /> when none is available or the read fails.
    /// </returns>
    /// <remarks>
    /// Declared <see langword="private protected" /> because <see cref="CachePairState" /> is an internal storage
    /// detail: the seam is open only to backends within this assembly. An out-of-assembly backend implements the public
    /// <see cref="IExchangeRateCache" /> contract directly instead, as <see cref="NullExchangeRateCache" /> does.
    /// </remarks>
    private protected abstract CachePairState ReadState(ExchangeRatePair pair);

    /// <summary>
    /// Writes the supplied state for a pair, replacing any existing state.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The state to persist.</param>
    /// <remarks>
    /// Declared <see langword="private protected" /> for the same reason as <see cref="ReadState" />:
    /// <see cref="CachePairState" /> is an internal storage detail shared only with same-assembly backends.
    /// </remarks>
    private protected abstract void WriteState(ExchangeRatePair pair, CachePairState state);

    /// <summary>
    /// Reports whether a cached row is semantically valid against the evaluation instant.
    /// </summary>
    /// <param name="entry">The cached row to validate.</param>
    /// <param name="asOf">
    /// The instant against which the caching instant is checked for implausible future stamps.
    /// </param>
    /// <returns>
    /// <see langword="false" /> when the row carries a non-positive rate, a default (unset) date, or a caching instant
    /// implausibly far in the future of <paramref name="asOf" />; otherwise <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// Invalid rows are silently skipped on both write (rejecting bad incoming data) and read (rejecting persisted or
    /// tampered rows) so a malformed cache never surfaces a nonsensical rate. A small clock-skew tolerance is allowed
    /// so a row stamped marginally ahead of the evaluating clock is not discarded.
    /// </remarks>
    private static bool IsValid(CachedExchangeRate entry, DateTimeOffset asOf) =>
        entry.Rate > 0m
            && entry.Date != default
            && entry.CachedAtUtc <= asOf + s_clockSkewTolerance;

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(ExchangeRatePair pair) =>
        _pairLocks.GetOrAdd(pair, static _ => new object());
}

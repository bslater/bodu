// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheBase{T}.cs" company="Bodu Pty. Ltd.">
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
/// sequences in <see cref="Store" />, <see cref="RecordCoverage" />, and <see cref="StoreFetchedRange" /> run under a
/// per-pair lock so concurrent writes to the same pair cannot interleave and lose either half. The lock is
/// process-local; file caches remain best-effort across processes, where the atomic temp-and-move write keeps each file
/// internally consistent.
/// </para>
/// <para>
/// The freshness, validity, merge, and coverage rules are not implemented here: they are delegated to the shared
/// <see cref="ExchangeRateCacheRules" /> so this base and the SQLite and distributed backends apply one authoritative
/// policy. This base contributes only the per-pair locking and the read-modify-write sequencing over a
/// <see cref="CachePairState" />.
/// </para>
/// </remarks>
public abstract class ExchangeRateCacheBase<TOptions>
    : IExchangeRateCache
    where TOptions : ExchangeRateCacheOptions
{
    /// <summary>The validated options carrying the bound provider and any storage settings.</summary>
    private readonly TOptions _options;

    /// <summary>The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" /> and <see cref="RecordCoverage" />. One lock object is created per pair on first use and reused thereafter.</summary>
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
    /// <value>The cache options.</value>
    protected TOptions Options => _options;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadState(pair).Entries;
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        return ExchangeRateCacheRules.SelectFresh(entries, duration, asOf);
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

            List<CachedExchangeRate> ordered = ExchangeRateCacheRules.MergeRows(state.Entries, rates, duration, asOf);

            // Preserve the existing coverage half: storing rows must never drop recorded coverage.
            WriteState(pair, new CachePairState(ordered, state.Coverage));
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf) =>
        ExchangeRateCacheRules.BuildCoverage(ToTuples(ReadState(pair).Coverage), duration, asOf);

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                ExchangeRateCacheRules.MergeCoverage(ToTuples(state.Coverage), start, end, duration, asOf);

            // Preserve the existing entries half: recording coverage must never drop cached rows.
            WriteState(pair, new CachePairState(state.Entries, ToWindows(windows)));
        }
    }

    /// <inheritdoc />
    public ExchangeRateCacheWriteStatus StoreFetchedRange(
        ExchangeRatePair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            // Merge both halves first, then write them together so a reader never observes coverage without its rows.
            List<CachedExchangeRate> ordered = ExchangeRateCacheRules.MergeRows(state.Entries, rows, duration, asOf);
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                ExchangeRateCacheRules.MergeCoverage(ToTuples(state.Coverage), start, end, duration, asOf);

            // Report the real persistence outcome: a swallowed storage failure must surface as Failed so the caller
            // refetches rather than trusting coverage that was never written.
            return WriteState(pair, new CachePairState(ordered, ToWindows(windows)))
                ? ExchangeRateCacheWriteStatus.Stored
                : ExchangeRateCacheWriteStatus.Failed;
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
    /// <returns>
    /// <see langword="true" /> when the state was persisted, including the deliberate deletion of an empty state;
    /// <see langword="false" /> when a storage failure was swallowed and nothing was persisted.
    /// </returns>
    /// <remarks>
    /// Declared <see langword="private protected" /> for the same reason as <see cref="ReadState" />:
    /// <see cref="CachePairState" /> is an internal storage detail shared only with same-assembly backends. The
    /// <see cref="bool" /> result lets <see cref="StoreFetchedRange" /> distinguish a durable write from a best-effort
    /// backend that swallowed an <see cref="IOException" /> or similar fault, so a failed write is reported as
    /// <see cref="ExchangeRateCacheWriteStatus.Failed" /> rather than falsely as
    /// <see cref="ExchangeRateCacheWriteStatus.Stored" />.
    /// </remarks>
    private protected abstract bool WriteState(ExchangeRatePair pair, CachePairState state);

    /// <summary>
    /// Projects the internal coverage windows into the plain tuples the shared <see cref="ExchangeRateCacheRules" />
    /// operate on.
    /// </summary>
    /// <param name="coverage">The coverage windows to project.</param>
    /// <returns>The windows as <c>(Start, End, FetchedAtUtc)</c> tuples.</returns>
    private static List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> ToTuples(IReadOnlyList<CoverageWindow> coverage)
    {
        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> tuples = new(coverage.Count);
        foreach (CoverageWindow window in coverage)
            tuples.Add((window.Start, window.End, window.FetchedAtUtc));

        return tuples;
    }

    /// <summary>
    /// Projects the plain coverage tuples produced by the shared <see cref="ExchangeRateCacheRules" /> back into the
    /// internal <see cref="CoverageWindow" /> representation persisted in a <see cref="CachePairState" />.
    /// </summary>
    /// <param name="tuples">The coverage tuples to project.</param>
    /// <returns>The tuples as <see cref="CoverageWindow" /> values.</returns>
    private static List<CoverageWindow> ToWindows(List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> tuples)
    {
        List<CoverageWindow> windows = new(tuples.Count);
        foreach ((DateOnly start, DateOnly end, DateTimeOffset fetchedAt) in tuples)
            windows.Add(new CoverageWindow(start, end, fetchedAt));

        return windows;
    }

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(ExchangeRatePair pair) =>
        _pairLocks.GetOrAdd(pair, static _ => new object());
}

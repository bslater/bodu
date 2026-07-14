// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheBase{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic mechanism for an <see cref="IRateCache" />: read-time freshness filtering, write-time
/// merge-and-prune, and the recording and pruning of coverage windows for a single provider. Derived types implement
/// only the persistence of a <see cref="CachePairState" />; this base prescribes no physical storage structure.
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
/// <see cref="RateCacheRules" /> so this base and the SQLite and distributed backends apply one authoritative policy.
/// This base contributes only the per-pair locking and the read-modify-write sequencing over a
/// <see cref="CachePairState" />.
/// </para>
/// </remarks>
public abstract class RateCacheBase<TOptions>
    : IRateCache, IRateCacheSnapshotReader
    where TOptions : RateCacheOptions
{
    /// <summary>The validated options carrying the bound provider and any storage settings.</summary>
    private readonly TOptions _options;

    /// <summary>The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" /> and <see cref="RecordCoverage" />.</summary>
    private readonly StripedLockSet<CurrencyPair> _pairLocks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and any storage settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected RateCacheBase(TOptions options)
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
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedRate> entries = ReadState(pair).Entries;
        if (entries.Count == 0)
            return Array.Empty<CachedRate>();

        // Backends persist MergeRows output, so on the hot read path every stored row is still fresh, valid, and
        // date-ordered — serve the stored list as-is instead of copying it. Stored lists are exposed read-only by the
        // in-box backends, so this cannot leak mutable cache state.
        return RateCacheRules.IsAllFreshOrdered(entries, duration, asOf)
            ? entries
            : RateCacheRules.SelectFresh(entries, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            List<CachedRate> ordered = RateCacheRules.MergeRows(state.Entries, rates, duration, asOf);

            // Preserve the existing coverage half: storing rows must never drop recorded coverage.
            WriteState(pair, new CachePairState(ordered, state.Coverage));
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        RateCacheRules.BuildCoverage(ToTuples(ReadState(pair).Coverage), duration, asOf);

    /// <inheritdoc />
    /// <remarks>
    /// Both halves come from one <see cref="ReadState" />, so a range lookup that would otherwise call
    /// <see cref="GetCoverage" /> and <see cref="GetRates" /> separately reads the persisted state once.
    /// </remarks>
    RateCacheSnapshot IRateCacheSnapshotReader.ReadSnapshot(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        CachePairState state = ReadState(pair);
        return new RateCacheSnapshot(state.Entries, RateCacheRules.BuildCoverage(ToTuples(state.Coverage), duration, asOf));
    }

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            CachePairState state = ReadState(pair);

            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                RateCacheRules.MergeCoverage(ToTuples(state.Coverage), start, end, duration, asOf);

            // Preserve the existing entries half: recording coverage must never drop cached rows.
            WriteState(pair, new CachePairState(state.Entries, ToWindows(windows)));
        }
    }

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> rows,
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
            List<CachedRate> ordered = RateCacheRules.MergeRows(state.Entries, rows, duration, asOf);
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                RateCacheRules.MergeCoverage(ToTuples(state.Coverage), start, end, duration, asOf);

            // Report the real persistence outcome: a swallowed storage failure must surface as Failed so the caller
            // refetches rather than trusting coverage that was never written.
            return WriteState(pair, new CachePairState(ordered, ToWindows(windows)))
                ? RateCacheWriteStatus.Stored
                : RateCacheWriteStatus.Failed;
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
    /// Declared <see langword="internal" /> so the storage seam is open to the backends in this assembly and — through
    /// the existing <c>InternalsVisibleTo</c> grants — to the companion distributed package, which stores a pair's
    /// whole state as one unit and derives from this base. A backend whose halves are independently addressable (the
    /// SQLite cache's two tables) or an unrelated third-party backend implements the public <see cref="IRateCache" />
    /// contract directly instead, as <see cref="NullRateCache" /> does.
    /// </remarks>
    internal abstract CachePairState ReadState(CurrencyPair pair);

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
    /// Declared <see langword="internal" /> for the same reason as <see cref="ReadState" />. The <see cref="bool" />
    /// result lets <see cref="StoreFetchedRange" /> distinguish a durable write from a best-effort backend that
    /// swallowed an <see cref="IOException" /> or similar fault, so a failed write is reported as
    /// <see cref="RateCacheWriteStatus.Failed" /> rather than falsely as <see cref="RateCacheWriteStatus.Stored" />.
    /// </remarks>
    internal abstract bool WriteState(CurrencyPair pair, CachePairState state);

    /// <summary>
    /// Projects the internal coverage windows into the plain tuples the shared <see cref="RateCacheRules" /> operate
    /// on.
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
    /// Projects the plain coverage tuples produced by the shared <see cref="RateCacheRules" /> back into the internal
    /// <see cref="CoverageWindow" /> representation persisted in a <see cref="CachePairState" />.
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
    private object LockFor(CurrencyPair pair) =>
        _pairLocks.For(pair);
}

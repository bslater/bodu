// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheRules.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic freshness, validity, merge, and coverage rules shared by every
/// <see cref="IExchangeRateCache" /> implementation, so the in-memory, file, SQLite, and distributed backends apply one
/// authoritative policy and differ only in how they read and write their bytes.
/// </summary>
/// <remarks>
/// <para>
/// Every method operates on the public <see cref="CachedExchangeRate" /> row type and on a plain
/// <c>(Start, End, FetchedAtUtc)</c> coverage tuple, so a backend can call these rules without exposing its internal
/// state representation. The rules are pure: they read their inputs, evaluate freshness and validity against the
/// supplied instant, and return new collections, leaving all persistence and locking to the caller.
/// </para>
/// <para>
/// Freshness is a strict less-than comparison — a row or window exactly one duration old is stale — and validity allows
/// a one-minute clock-skew tolerance so a row stamped marginally ahead of the evaluating clock is not discarded. These
/// are the same thresholds the cache surface has always applied; centralising them here keeps every backend identical,
/// which the shared cache contract tests assert.
/// </para>
/// </remarks>
public static class ExchangeRateCacheRules
{
    /// <summary>
    /// The clock-skew tolerance applied when validating a row's caching instant: a row stamped more than this far in
    /// the future of the evaluation instant is treated as invalid rather than fresh.
    /// </summary>
    private static readonly TimeSpan s_clockSkewTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reports whether a cached row is semantically valid against the evaluation instant.
    /// </summary>
    /// <param name="row">The cached row to validate.</param>
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
    public static bool IsValid(CachedExchangeRate row, DateTimeOffset asOf) =>
        row.Rate > 0m
            && row.Date != default
            && row.CachedAtUtc <= asOf + s_clockSkewTolerance;

    /// <summary>
    /// Selects the rows that are both semantically valid and still fresh at <paramref name="asOf" />, ordered by date.
    /// </summary>
    /// <param name="rows">The candidate rows to filter.</param>
    /// <param name="duration">The duration a cached row remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which freshness and validity are evaluated.</param>
    /// <returns>The fresh, valid rows ordered ascending by date; empty when none qualify.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rows" /> is <see langword="null" />.
    /// </exception>
    public static List<CachedExchangeRate> SelectFresh(IEnumerable<CachedExchangeRate> rows, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);

        List<CachedExchangeRate> fresh = new();
        foreach (CachedExchangeRate row in rows)
        {
            if (IsValid(row, asOf) && row.IsFresh(asOf, duration))
                fresh.Add(row);
        }

        fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return fresh;
    }

    /// <summary>
    /// Merges <paramref name="incoming" /> rows into <paramref name="existing" /> rows so the most recently cached row
    /// wins per date, dropping rows that are stale or semantically invalid, and ordering the result by date.
    /// </summary>
    /// <param name="existing">The rows already stored for the pair.</param>
    /// <param name="incoming">The rows being stored, which take precedence on a tie by caching instant.</param>
    /// <param name="duration">The duration a cached row remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which staleness and validity are evaluated.</param>
    /// <returns>The merged, pruned rows ordered ascending by date; empty when none survive.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="existing" /> or <paramref name="incoming" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// An incoming invalid row is skipped before it can overwrite a valid stored row for the same date. After the
    /// merge, every surviving row is re-checked for freshness and validity so the store self-cleans on each write.
    /// </remarks>
    public static List<CachedExchangeRate> MergeRows(
        IEnumerable<CachedExchangeRate> existing,
        IEnumerable<CachedExchangeRate> incoming,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(existing);
        ThrowHelper.ThrowIfNull(incoming);

        // Merge with any existing entry so the most recently cached rate wins per date.
        Dictionary<DateOnly, CachedExchangeRate> merged = new();
        foreach (CachedExchangeRate row in existing)
            merged[row.Date] = row;

        foreach (CachedExchangeRate row in incoming)
        {
            if (!IsValid(row, asOf))
                continue;

            if (!merged.TryGetValue(row.Date, out CachedExchangeRate current) || row.CachedAtUtc >= current.CachedAtUtc)
                merged[row.Date] = row;
        }

        // Prune rows that are no longer fresh or are semantically invalid, then order by date so the store is stable and
        // self-cleaning.
        List<CachedExchangeRate> ordered = new(merged.Count);
        foreach (CachedExchangeRate row in merged.Values)
        {
            if (IsValid(row, asOf) && row.IsFresh(asOf, duration))
                ordered.Add(row);
        }

        ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return ordered;
    }

    /// <summary>
    /// Folds the still-fresh coverage windows for a pair into a <see cref="DateRangeCoverage" />, evaluated against
    /// <paramref name="asOf" />.
    /// </summary>
    /// <param name="windows">The recorded coverage windows to fold.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <param name="asOf">The instant against which coverage freshness is evaluated.</param>
    /// <returns>
    /// A <see cref="DateRangeCoverage" /> describing the days known to have been fetched and still fresh; empty when no
    /// fresh window remains.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="windows" /> is <see langword="null" />.
    /// </exception>
    public static DateRangeCoverage BuildCoverage(
        IEnumerable<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(windows);

        DateRangeCoverage coverage = new();
        foreach ((DateOnly start, DateOnly end, DateTimeOffset fetchedAt) in windows)
        {
            if (asOf - fetchedAt < duration)
                coverage.Add(start, end);
        }

        return coverage;
    }

    /// <summary>
    /// Appends the newly fetched window <paramref name="start" />..<paramref name="end" /> stamped at
    /// <paramref name="asOf" /> to the still-fresh existing windows, dropping windows that are no longer fresh.
    /// </summary>
    /// <param name="existing">The windows already recorded for the pair.</param>
    /// <param name="start">The inclusive first date of the newly fetched range.</param>
    /// <param name="end">The inclusive last date of the newly fetched range.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <param name="asOf">
    /// The instant the new window is stamped with and against which stale windows are pruned.
    /// </param>
    /// <returns>The pruned existing windows with the new window appended.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="existing" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public static List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> MergeCoverage(
        IEnumerable<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> existing,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(existing);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        // Keep the still-fresh windows, drop the rest, then append the newly fetched window so the store self-cleans.
        // A still-fresh window fully covered by the newly fetched (and therefore fresher) window is also dropped: those
        // days were just re-observed at asOf, so the new window supersedes it. This bounds growth under the common
        // pattern of repeatedly refetching the same or a widening range, without coalescing windows whose distinct fetch
        // instants must each drive their own expiry.
        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows = new();
        foreach ((DateOnly windowStart, DateOnly windowEnd, DateTimeOffset fetchedAt) in existing)
        {
            if (asOf - fetchedAt >= duration)
                continue;

            if (windowStart >= start && windowEnd <= end)
                continue;

            windows.Add((windowStart, windowEnd, fetchedAt));
        }

        windows.Add((start, end, asOf));
        return windows;
    }
}

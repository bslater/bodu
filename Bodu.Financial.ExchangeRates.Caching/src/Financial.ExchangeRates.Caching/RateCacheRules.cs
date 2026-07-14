// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRules.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic freshness, validity, merge, and coverage rules shared by every
/// <see cref="IRateCache" /> implementation, so the in-memory, file, SQLite, and distributed backends apply one
/// authoritative policy and differ only in how they read and write their bytes.
/// </summary>
/// <remarks>
/// <para>
/// Every method operates on the public <see cref="CachedRate" /> row type and on a plain
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
public static class RateCacheRules
{
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
    public static bool IsValid(CachedRate row, DateTimeOffset asOf) =>
        row.Rate > 0m
            && row.Date != default
            && CacheFreshness.IsWithinClockSkew(row.CachedAtUtc, asOf);

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
    public static List<CachedRate> SelectFresh(IEnumerable<CachedRate> rows, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);

        // Track ordering while filtering so the sort — an O(n log n) pass — runs only for an unsorted source. Every
        // backend persists rows already date-ordered by MergeRows, so on the hot read path the surviving rows arrive
        // sorted and the scan below is the only cost.
        List<CachedRate> fresh = new();
        var sorted = true;
        var lastDate = DateOnly.MinValue;
        foreach (CachedRate row in rows)
        {
            if (IsValid(row, asOf) && row.IsFresh(asOf, duration))
            {
                if (row.Date < lastDate)
                    sorted = false;

                lastDate = row.Date;
                fresh.Add(row);
            }
        }

        if (!sorted)
            fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));

        return fresh;
    }

    /// <summary>
    /// Reports whether every row is valid, fresh at <paramref name="asOf" />, and already ordered ascending by date —
    /// the condition under which a read can serve the stored list as-is instead of building the filtered copy
    /// <see cref="SelectFresh" /> would produce.
    /// </summary>
    /// <param name="rows">The stored rows to inspect.</param>
    /// <param name="duration">The duration a cached row remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which freshness and validity are evaluated.</param>
    /// <returns>
    /// <see langword="true" /> when serving <paramref name="rows" /> unchanged is equivalent to
    /// <see cref="SelectFresh" />'s output; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Every backend persists <see cref="MergeRows" /> output — valid, fresh-at-write, date-ordered — so on the hot
    /// read path this check passes and the read is allocation-free. An empty list trivially qualifies.
    /// </remarks>
    internal static bool IsAllFreshOrdered(IReadOnlyList<CachedRate> rows, TimeSpan duration, DateTimeOffset asOf)
    {
        var lastDate = DateOnly.MinValue;
        for (int i = 0; i < rows.Count; i++)
        {
            CachedRate row = rows[i];
            if (!IsValid(row, asOf) || !row.IsFresh(asOf, duration) || row.Date < lastDate)
                return false;

            lastDate = row.Date;
        }

        return true;
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
    public static List<CachedRate> MergeRows(
        IEnumerable<CachedRate> existing,
        IEnumerable<CachedRate> incoming,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(existing);
        ThrowHelper.ThrowIfNull(incoming);

        // The dominant store shape is a single-day observation merged into an already sorted, duplicate-free stored
        // list (every backend persists this method's own output). That case merges in one linear pass with no
        // dictionary; any input that breaks the sorted-distinct precondition falls through to the general path.
        if (incoming is IReadOnlyList<CachedRate> { Count: 1 } singleIncoming
            && existing is IReadOnlyList<CachedRate> existingList
            && TryMergeSingle(existingList, singleIncoming[0], duration, asOf, out List<CachedRate>? fastMerged))
        {
            return fastMerged;
        }

        // Merge with any existing entry so the most recently cached rate wins per date.
        Dictionary<DateOnly, CachedRate> merged = new();
        foreach (CachedRate row in existing)
            merged[row.Date] = row;

        foreach (CachedRate row in incoming)
        {
            if (!IsValid(row, asOf))
                continue;

            if (!merged.TryGetValue(row.Date, out CachedRate current) || row.CachedAtUtc >= current.CachedAtUtc)
                merged[row.Date] = row;
        }

        // Prune rows that are no longer fresh or are semantically invalid, then order by date so the store is stable and
        // self-cleaning.
        List<CachedRate> ordered = new(merged.Count);
        foreach (CachedRate row in merged.Values)
        {
            if (IsValid(row, asOf) && row.IsFresh(asOf, duration))
                ordered.Add(row);
        }

        ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return ordered;
    }

    /// <summary>
    /// Attempts the single-incoming-row merge in one linear pass over a date-sorted, duplicate-free existing list,
    /// producing the same pruned, ordered result as the general dictionary merge without building the dictionary.
    /// </summary>
    /// <param name="existing">The rows already stored for the pair, expected date-sorted and duplicate-free.</param>
    /// <param name="incoming">The single row being stored.</param>
    /// <param name="duration">The duration a cached row remains fresh after it was cached.</param>
    /// <param name="asOf">The instant against which staleness and validity are evaluated.</param>
    /// <param name="merged">The merged, pruned rows when the fast path applied.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="existing" /> satisfied the sorted-distinct precondition and
    /// <paramref name="merged" /> holds the result; <see langword="false" /> when the caller must run the general
    /// merge because the precondition does not hold.
    /// </returns>
    /// <remarks>
    /// The general merge keeps the last existing occurrence per date; a duplicate date in <paramref name="existing" />
    /// therefore aborts the fast path rather than silently keeping the first occurrence. The incoming row overwrites an
    /// existing same-date row only when it is valid and at least as recently cached, matching the dictionary path.
    /// </remarks>
    private static bool TryMergeSingle(
        IReadOnlyList<CachedRate> existing,
        CachedRate incoming,
        TimeSpan duration,
        DateTimeOffset asOf,
        out List<CachedRate>? merged)
    {
        var incomingValid = IsValid(incoming, asOf);
        var incomingSurvives = incomingValid && incoming.IsFresh(asOf, duration);
        List<CachedRate> result = new(existing.Count + 1);
        var lastDate = DateOnly.MinValue;
        var placed = false;

        for (int i = 0; i < existing.Count; i++)
        {
            CachedRate row = existing[i];

            // The precondition is strictly ascending dates; an equal or earlier date means the input was not produced
            // by these rules, so the general merge must arbitrate it.
            if (i > 0 && row.Date <= lastDate)
            {
                merged = null;
                return false;
            }

            lastDate = row.Date;

            if (!placed && incoming.Date < row.Date)
            {
                if (incomingSurvives)
                    result.Add(incoming);
                placed = true;
            }

            if (row.Date == incoming.Date)
            {
                CachedRate winner = incomingValid && incoming.CachedAtUtc >= row.CachedAtUtc ? incoming : row;
                if (IsValid(winner, asOf) && winner.IsFresh(asOf, duration))
                    result.Add(winner);
                placed = true;
                continue;
            }

            if (IsValid(row, asOf) && row.IsFresh(asOf, duration))
                result.Add(row);
        }

        if (!placed && incomingSurvives)
            result.Add(incoming);

        merged = result;
        return true;
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

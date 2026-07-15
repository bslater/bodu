// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheRules.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the storage-agnostic freshness, validity, version-matching, and merge rules shared by every
/// <see cref="INotableDateCache" /> implementation, so the in-memory, file, SQLite, and distributed backends apply one
/// authoritative policy and differ only in how they read and write their bytes.
/// </summary>
/// <remarks>
/// Freshness is a strict less-than comparison — an entry exactly one time-to-live old is stale — and validity allows a
/// small clock-skew tolerance so an entry stamped marginally ahead of the evaluating clock is not discarded. An entry
/// is served only when its resource version matches the requested version, and a store drops entries whose version
/// differs from the one being written, so a resource reload wholesale invalidates a territory's cache.
/// </remarks>
internal static class NotableDateCacheRules
{
    /// <summary>The smallest permitted civil year, matching the notable-date engine's own lower bound.</summary>
    private const int MinYear = 1;

    /// <summary>The largest permitted civil year, matching the notable-date engine's own upper bound.</summary>
    private const int MaxYear = 9999;

    /// <summary>
    /// Reports whether a cache entry is semantically valid against the evaluation instant.
    /// </summary>
    /// <param name="entry">The entry to validate.</param>
    /// <param name="asOf">
    /// The instant against which the computed instant is checked for implausible future stamps.
    /// </param>
    /// <returns>
    /// <see langword="false" /> when the entry carries a null territory or version, a year outside the supported range,
    /// or a computed instant implausibly far in the future of <paramref name="asOf" />; otherwise
    /// <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// Invalid entries are skipped on both write (rejecting bad incoming data) and read (rejecting persisted or
    /// tampered entries) so a malformed cache never surfaces a nonsensical result.
    /// </remarks>
    public static bool IsValid(NotableDateCacheEntry entry, DateTimeOffset asOf) =>
        entry is not null
            && entry.Territory is not null
            && entry.ResourceVersion is not null
            && entry.Occurrences is not null
            && entry.Year is >= MinYear and <= MaxYear
            && CacheFreshness.IsWithinClockSkew(entry.ComputedAtUtc, asOf);

    /// <summary>
    /// Selects the cached entry for a civil year and resource version that is both valid and still fresh at
    /// <paramref name="asOf" />, or <see langword="null" /> when none qualifies.
    /// </summary>
    /// <param name="entries">The candidate entries for the territory.</param>
    /// <param name="year">The civil year to select.</param>
    /// <param name="resourceVersion">The resource version the entry must match.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which freshness and validity are evaluated.</param>
    /// <returns>The fresh, version-matching entry, or <see langword="null" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries" /> is <see langword="null" />.
    /// </exception>
    public static NotableDateCacheEntry? SelectFresh(
        IReadOnlyList<NotableDateCacheEntry> entries,
        int year,
        string resourceVersion,
        TimeSpan ttl,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(entries);

        for (int i = 0; i < entries.Count; i++)
        {
            NotableDateCacheEntry entry = entries[i];
            if (entry.Year == year
                && string.Equals(entry.ResourceVersion, resourceVersion, StringComparison.Ordinal)
                && IsValid(entry, asOf)
                && entry.IsFresh(asOf, ttl))
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// Selects the fresh, version-matching entry for every civil year in <paramref name="years" />'s span in a single
    /// pass over the candidate entries, writing each match into its year's slot.
    /// </summary>
    /// <param name="entries">The candidate entries for the territory.</param>
    /// <param name="firstYear">The civil year the first slot of <paramref name="years" /> represents.</param>
    /// <param name="resourceVersion">The resource version each entry must match.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which freshness and validity are evaluated.</param>
    /// <param name="years">
    /// The per-year result slots, one per year starting at <paramref name="firstYear" />; a slot is left
    /// <see langword="null" /> when no entry qualifies for its year.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries" /> or <paramref name="years" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to calling <see cref="SelectFresh" /> once per year, but scanning the entry list once rather than
    /// once per year — the batch read path over a multi-year span is O(entries) instead of O(years × entries).
    /// </remarks>
    public static void SelectFreshInto(
        IReadOnlyList<NotableDateCacheEntry> entries,
        int firstYear,
        string resourceVersion,
        TimeSpan ttl,
        DateTimeOffset asOf,
        NotableDateCacheEntry?[] years)
    {
        ThrowHelper.ThrowIfNull(entries);
        ThrowHelper.ThrowIfNull(years);

        int lastYear = firstYear + years.Length - 1;
        for (int i = 0; i < entries.Count; i++)
        {
            NotableDateCacheEntry entry = entries[i];
            if (entry.Year >= firstYear
                && entry.Year <= lastYear
                && years[entry.Year - firstYear] is null
                && string.Equals(entry.ResourceVersion, resourceVersion, StringComparison.Ordinal)
                && IsValid(entry, asOf)
                && entry.IsFresh(asOf, ttl))
            {
                years[entry.Year - firstYear] = entry;
            }
        }
    }

    /// <summary>
    /// Merges <paramref name="incoming" /> into <paramref name="existing" /> so the incoming year replaces any existing
    /// entry for the same year, dropping entries that are stale, invalid, or belong to a superseded resource version,
    /// and ordering the result by year.
    /// </summary>
    /// <param name="existing">The entries already stored for the territory.</param>
    /// <param name="incoming">The computed year being stored.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which staleness and validity are evaluated.</param>
    /// <returns>The merged, pruned entries ordered ascending by year; empty when none survive.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="existing" /> or <paramref name="incoming" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Existing entries whose version differs from <paramref name="incoming" /> are dropped: a resource reload changes
    /// the version token, so a store under the new version supersedes every entry from the old one. After the merge,
    /// every surviving entry is re-checked for freshness and validity so the store self-cleans on each write.
    /// </remarks>
    public static List<NotableDateCacheEntry> Merge(
        IReadOnlyList<NotableDateCacheEntry> existing,
        NotableDateCacheEntry incoming,
        TimeSpan ttl,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(existing);
        ThrowHelper.ThrowIfNull(incoming);

        List<NotableDateCacheEntry> merged = new(existing.Count + 1);
        if (IsValid(incoming, asOf))
            merged.Add(incoming);

        for (int i = 0; i < existing.Count; i++)
        {
            NotableDateCacheEntry entry = existing[i];

            // Drop the entry the incoming year replaces, any superseded-version entry, and anything no longer fresh or
            // valid, so the store keeps only current, live years.
            if (entry.Year == incoming.Year)
                continue;

            if (!string.Equals(entry.ResourceVersion, incoming.ResourceVersion, StringComparison.Ordinal))
                continue;

            if (IsValid(entry, asOf) && entry.IsFresh(asOf, ttl))
                merged.Add(entry);
        }

        merged.Sort(static (left, right) => left.Year.CompareTo(right.Year));
        return merged;
    }

    /// <summary>
    /// Normalizes a territory code to its canonical cache-key form: trimmed and upper-cased with the invariant culture,
    /// matching the case-insensitive territory matching the notable-date engine applies.
    /// </summary>
    /// <param name="territory">The territory code to normalize.</param>
    /// <returns>The normalized territory code.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static string NormalizeTerritory(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        return territory.Trim().ToUpperInvariant();
    }
}

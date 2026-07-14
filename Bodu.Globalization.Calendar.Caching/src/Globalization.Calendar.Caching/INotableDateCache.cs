// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Persists computed notable-date resolutions so an expensive per-year resolution need not be recomputed while it
/// remains fresh, keyed by territory, civil year, and resource version.
/// </summary>
/// <remarks>
/// <para>
/// The cache stores one <see cref="NotableDateCacheEntry" /> per <c>(territory, year, resource version)</c>: the
/// unfiltered occurrences the notable-date engine emits for a whole civil year. A range query is served by assembling
/// the per-year entries it spans and clipping to the requested window, so the cache unit is always a single civil year.
/// </para>
/// <para>
/// The cache owns expiry. A caller supplies a time-to-live and an evaluation instant on every call: <see cref="GetYear" />
/// returns an entry only while it is fresh, and <see cref="StoreYear" /> prunes stale and superseded-version entries as
/// it merges, so the backing store self-cleans over time. An entry is served only when its
/// <see cref="NotableDateCacheEntry.ResourceVersion" /> matches the requested version, so a resource reload — signalled
/// by a changed version token — forces a recompute rather than serving stale data.
/// </para>
/// <para>
/// Territory is normalized case-insensitively for keying, so <c>us</c> and <c>US</c> address the same entry; a
/// subdivision (<c>CA-ON</c>) and its parent country (<c>CA</c>) are distinct keys, matching the engine's own
/// resolution.
/// </para>
/// <para>
/// <strong>Ordering contract.</strong> An entry's occurrences must round-trip in the order they were supplied to
/// <see cref="StoreYear" /> — the notable-date service's date-then-identity order. The caching service relies on this to
/// assemble ordered range results without re-sorting; a backend that cannot preserve order forces a sort fallback on
/// every read it serves.
/// </para>
/// <para>
/// Implementations are expected to be resilient: a cache failure should manifest as an empty read or a no-op write
/// rather than an exception that breaks notable-date resolution. Argument validation still throws.
/// </para>
/// </remarks>
public interface INotableDateCache
{
    /// <summary>
    /// Returns the cached entry for the requested territory, civil year, and resource version when one is present and
    /// still fresh, evaluated against <paramref name="asOf" />.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="resourceVersion">The version token of the resource currently in effect.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <returns>
    /// The fresh, version-matching cached entry, or <see langword="null" /> when none is available, fresh, or
    /// version-matching.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="territory" /> or <paramref name="resourceVersion" /> is <see langword="null" />.
    /// </exception>
    NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf);

    /// <summary>
    /// Stores a computed year, merging it into the territory's cached entries so the most recently computed entry wins
    /// per year, and pruning entries that are stale or belong to a superseded resource version.
    /// </summary>
    /// <param name="entry">The computed year to store.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which stale entries are pruned.</param>
    /// <returns>
    /// <see cref="NotableDateCacheWriteStatus.Stored" /> when the entry was persisted;
    /// <see cref="NotableDateCacheWriteStatus.Failed" /> when a storage error was swallowed and nothing was persisted;
    /// <see cref="NotableDateCacheWriteStatus.Skipped" /> for a cache that intentionally stores nothing.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry" /> is <see langword="null" />.</exception>
    NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf);

    /// <summary>
    /// Removes every cached entry, returning the cache to its empty state.
    /// </summary>
    /// <remarks>
    /// This is a best-effort operation: a backing-store failure is swallowed rather than thrown, consistent with the
    /// rest of the contract.
    /// </remarks>
    void Clear();
}

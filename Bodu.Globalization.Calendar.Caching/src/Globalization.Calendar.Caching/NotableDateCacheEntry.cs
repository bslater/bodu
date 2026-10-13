// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Represents one cached unit of notable-date resolution: every occurrence in effect during a single civil year for a
/// single territory under a single resource version, together with the UTC instant the year was computed, which drives
/// expiry.
/// </summary>
/// <param name="Territory">
/// The territory the occurrences were resolved for, as the caller supplied it; a cache normalizes it for keying.
/// </param>
/// <param name="Year">The civil year the occurrences are in effect during.</param>
/// <param name="ResourceVersion">
/// The version token of the resource the occurrences were computed from; a change invalidates the entry.
/// </param>
/// <param name="Occurrences">
/// The occurrences whose span overlaps <paramref name="Year" />, including a multi-day occurrence that starts in the
/// previous year, unfiltered and in the order they were computed (the notable-date service's date-then-identity order),
/// which backends preserve per the <see cref="INotableDateCache" /> ordering contract.
/// </param>
/// <param name="ComputedAtUtc">The UTC instant at which the year was computed and written to the cache.</param>
/// <remarks>
/// <para>
/// A civil year is the natural cache unit because the notable-date engine computes per Gregorian year: resolving one
/// year is a pure function of the resource, the territory, and the year. A range query is assembled from the per-year
/// entries it spans, then clipped to the requested window by each occurrence's span, so an occurrence that starts
/// before the window and is still in effect inside it is kept.
/// </para>
/// <para>
/// An entry with an empty <see cref="Occurrences" /> list is a valid, cacheable result: it records that the year was
/// computed and yielded no notable dates, so a later query is served rather than recomputed. Freshness is governed both
/// by <see cref="ComputedAtUtc" /> against a time-to-live and by <see cref="ResourceVersion" /> against the resource
/// currently in effect.
/// </para>
/// </remarks>
public sealed record NotableDateCacheEntry(
    string Territory,
    int Year,
    string ResourceVersion,
    IReadOnlyList<NotableDate> Occurrences,
    DateTimeOffset ComputedAtUtc)
{
    /// <summary>
    /// Reports whether the entry is still fresh at <paramref name="asOf" /> under the supplied time-to-live.
    /// </summary>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="asOf" /> is within <paramref name="ttl" /> of
    /// <see cref="ComputedAtUtc" />; otherwise <see langword="false" />.
    /// </returns>
    public bool IsFresh(DateTimeOffset asOf, TimeSpan ttl) =>
        CacheFreshness.IsFresh(ComputedAtUtc, ttl, asOf);
}

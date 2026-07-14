// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCacheBatchReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An optional read seam for <see cref="INotableDateCache" /> backends that can answer a span of civil years from one
/// state read, letting the caching service's multi-year range path avoid re-reading the territory's persisted state
/// once per year.
/// </summary>
/// <remarks>
/// <para>
/// A multi-year range query needs each spanned year's entry. Looking each year up individually through
/// <see cref="INotableDateCache.GetYear" /> re-reads the territory's whole persisted state per year on a backend that
/// stores the territory as one unit — the distributed cache re-fetches and re-deserializes the same blob, and a file
/// cache re-stats and re-scans the same parse memo. Implementing this seam collapses those reads to one: the batch is
/// answered from a single territory read, applying the same per-year freshness and version policy as
/// <see cref="INotableDateCache.GetYear" />.
/// </para>
/// <para>
/// The seam is optional. The caching service falls back to per-year <see cref="INotableDateCache.GetYear" /> calls for
/// any cache that does not implement it, so third-party backends are unaffected.
/// </para>
/// </remarks>
internal interface INotableDateCacheBatchReader
{
    /// <summary>
    /// Returns the cached entries for every civil year in the inclusive span, each evaluated with the same freshness
    /// and version policy as <see cref="INotableDateCache.GetYear" />, from one territory read.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="firstYear">The inclusive first civil year of the span.</param>
    /// <param name="lastYear">The inclusive last civil year of the span.</param>
    /// <param name="resourceVersion">The version token of the resource currently in effect.</param>
    /// <param name="ttl">The duration a computed year remains fresh after it was computed.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <returns>
    /// One element per year in span order — the fresh, version-matching entry for that year, or <see langword="null" />
    /// when the year misses.
    /// </returns>
    NotableDateCacheEntry?[] GetYears(string territory, int firstYear, int lastYear, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf);
}

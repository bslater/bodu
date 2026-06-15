// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CoverageWindow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Records one inclusive date range that was actually fetched from a provider, together with the UTC instant the fetch
/// occurred, which drives expiry of the coverage just as <see cref="CachedExchangeRate.CachedAtUtc" /> drives expiry of
/// a cached rate.
/// </summary>
/// <param name="Start">The inclusive first date of the fetched range.</param>
/// <param name="End">The inclusive last date of the fetched range.</param>
/// <param name="FetchedAtUtc">The UTC instant at which the range was fetched and recorded as covered.</param>
/// <remarks>
/// A coverage window answers the question that a rate row alone cannot: which days were <em>requested</em> from the
/// provider, including days that returned no observation (weekends, holidays, gaps). Persisting these windows lets a
/// range lookup distinguish "the cache has no row for this interior day because none exists" from "this interior day
/// was never fetched", so a window that straddles an unfetched gap is correctly refetched rather than served from a
/// sparse cache.
/// </remarks>
internal readonly record struct CoverageWindow(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)
{
    /// <summary>
    /// Reports whether the recorded coverage is still fresh at <paramref name="asOf" /> under the supplied caching
    /// duration.
    /// </summary>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="asOf" /> is within <paramref name="duration" /> of
    /// <see cref="FetchedAtUtc" />; otherwise <see langword="false" />.
    /// </returns>
    public bool IsFresh(DateTimeOffset asOf, TimeSpan duration) => asOf - FetchedAtUtc < duration;
}

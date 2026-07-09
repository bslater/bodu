// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Represents one cached exchange-rate observation: a dated rate together with the UTC instant it was cached, which
/// drives expiry, and the optional UTC instant the upstream data backing it was originally fetched.
/// </summary>
/// <param name="Date">The observation date of the rate.</param>
/// <param name="Rate">The observed rate for <paramref name="Date" />.</param>
/// <param name="CachedAtUtc">The UTC instant at which the rate was written to the cache.</param>
/// <param name="ObservedAtUtc">
/// The UTC instant the upstream data backing this rate was originally fetched, distinct from
/// <paramref name="CachedAtUtc" /> the cache-write instant; <see langword="null" /> when the source did not supply it.
/// </param>
public readonly record struct CachedRate(DateOnly Date, decimal Rate, DateTimeOffset CachedAtUtc, DateTimeOffset? ObservedAtUtc = null)
{
    /// <summary>
    /// Reports whether the cached rate is still fresh at <paramref name="asOf" /> under the supplied caching duration.
    /// </summary>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="duration">The duration a cached rate remains fresh after it was cached.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="asOf" /> is within <paramref name="duration" /> of
    /// <see cref="CachedAtUtc" />; otherwise <see langword="false" />.
    /// </returns>
    public bool IsFresh(DateTimeOffset asOf, TimeSpan duration) => asOf - CachedAtUtc < duration;
}

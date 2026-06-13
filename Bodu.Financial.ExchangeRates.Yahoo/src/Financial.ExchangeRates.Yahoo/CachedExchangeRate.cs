// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Represents one cached exchange-rate observation in the provider's common, provider- and pair-keyed cache format: a
/// dated rate together with the time it was retrieved, which drives expiration.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="Rate">The observed rate for <paramref name="Date" />.</param>
/// <param name="RetrievedAt">The instant at which the rate was retrieved from the source.</param>
public readonly record struct CachedExchangeRate(DateOnly Date, decimal Rate, DateTimeOffset RetrievedAt)
{
    /// <summary>
    /// Reports whether the rate is still fresh at <paramref name="asOf" /> under the supplied expiry.
    /// </summary>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="expiry">The duration a retrieved rate remains fresh.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="asOf" /> is within <paramref name="expiry" /> of
    /// <see cref="RetrievedAt" />; otherwise <see langword="false" />.
    /// </returns>
    public bool IsFresh(DateTimeOffset asOf, TimeSpan expiry) => asOf - RetrievedAt < expiry;
}

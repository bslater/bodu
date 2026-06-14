// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The mutable serialization shape of one cached rate row, distinct from the public <see cref="CachedExchangeRate" />
/// value so the on-disk format can evolve independently of the in-memory contract.
/// </summary>
public sealed class ExchangeRateCacheEntry
{
    /// <summary>
    /// Gets or sets the observation date of the rate.
    /// </summary>
    /// <returns>The observation date.</returns>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the observed rate for <see cref="Date" />.
    /// </summary>
    /// <returns>The observed rate.</returns>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant at which the rate was written to the cache.
    /// </summary>
    /// <returns>The caching instant.</returns>
    public DateTimeOffset CachedAtUtc { get; set; }
}

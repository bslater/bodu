// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The mutable serialization shape of one cached rate row, distinct from the public <see cref="CachedRate" /> value so
/// the on-disk format can evolve independently of the in-memory contract.
/// </summary>
public sealed class RateCacheEntry
{
    /// <summary>
    /// Gets or sets the observation date of the rate.
    /// </summary>
    /// <value>The observation date.</value>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the observed rate for <see cref="Date" />.
    /// </summary>
    /// <value>The observed rate.</value>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant at which the rate was written to the cache.
    /// </summary>
    /// <value>The caching instant.</value>
    public DateTimeOffset CachedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant the upstream data backing the rate was originally fetched, or
    /// <see langword="null" /> when the source did not supply it.
    /// </summary>
    /// <value>The upstream fetch instant, or <see langword="null" /> when not tracked.</value>
    /// <remarks>
    /// A cache file written before this field was tracked has no <c>ObservedAtUtc</c> key; it deserializes to
    /// <see langword="null" /> with no error, mirroring how a pre-coverage file deserializes its missing
    /// <c>[[Coverage]]</c> array, so older caches remain readable.
    /// </remarks>
    public DateTimeOffset? ObservedAtUtc { get; set; }
}

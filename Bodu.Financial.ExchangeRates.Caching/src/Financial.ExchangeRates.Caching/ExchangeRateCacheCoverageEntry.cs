// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheCoverageEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The mutable serialization shape of one recorded coverage window, distinct from the internal
/// <see cref="CoverageWindow" /> value so the on-disk format can evolve independently of the in-memory contract.
/// </summary>
/// <remarks>
/// A coverage entry records that the inclusive range <see cref="Start" />..<see cref="End" /> was fetched from the
/// provider at <see cref="FetchedAtUtc" />, which drives expiry of the coverage the same way a cached row's caching
/// instant drives expiry of the rate.
/// </remarks>
public sealed class ExchangeRateCacheCoverageEntry
{
    /// <summary>
    /// Gets or sets the inclusive first date of the fetched range.
    /// </summary>
    /// <returns>The first date covered by the window.</returns>
    public DateOnly Start { get; set; }

    /// <summary>
    /// Gets or sets the inclusive last date of the fetched range.
    /// </summary>
    /// <returns>The last date covered by the window.</returns>
    public DateOnly End { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant at which the range was fetched and recorded as covered.
    /// </summary>
    /// <returns>The instant the window was recorded.</returns>
    public DateTimeOffset FetchedAtUtc { get; set; }
}

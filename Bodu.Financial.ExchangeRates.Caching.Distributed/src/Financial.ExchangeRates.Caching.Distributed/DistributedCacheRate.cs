// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// The JSON serialization shape of one cached rate row, distinct from the public <see cref="CachedExchangeRate" />
/// value so the wire format can evolve independently of the in-memory contract.
/// </summary>
/// <remarks>
/// All three fields are persisted as invariant text so the row round-trips losslessly regardless of the serializer's
/// default number and date handling: the rate as an invariant decimal string preserves full precision and scale, the
/// date as <c>yyyy-MM-dd</c>, and the caching instant in round-trip (<c>"O"</c>) form preserves its offset and
/// sub-second precision.
/// </remarks>
internal sealed class DistributedCacheRate
{
    /// <summary>
    /// Gets or sets the observation date of the rate, formatted as invariant <c>yyyy-MM-dd</c> text.
    /// </summary>
    /// <returns>The observation date as invariant ISO text.</returns>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observed rate, formatted as an invariant decimal string so its precision and scale round-trip
    /// exactly.
    /// </summary>
    /// <returns>The observed rate as invariant decimal text.</returns>
    public string Rate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant at which the rate was written to the cache, formatted as invariant round-trip (
    /// <c>"O"</c>) text.
    /// </summary>
    /// <returns>The caching instant as invariant round-trip text.</returns>
    public string CachedAtUtc { get; set; } = string.Empty;
}

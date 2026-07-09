// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The JSON serialization shape of one cached rate row, distinct from the public <see cref="CachedRate" /> value so the
/// wire format can evolve independently of the in-memory contract.
/// </summary>
/// <remarks>
/// Every field is persisted as invariant text so the row round-trips losslessly regardless of the serializer's default
/// number and date handling: the rate as an invariant decimal string preserves full precision and scale, the date as
/// <c>yyyy-MM-dd</c>, and the caching and upstream fetch instants in round-trip (<c>"O"</c>) form preserve their offset
/// and sub-second precision. The optional <see cref="ObservedAtUtc" /> is omitted from the JSON when
/// <see langword="null" />, so a legacy-shaped blob without it stays minimal and reads back as a null fetch instant.
/// </remarks>
internal sealed class DistributedCacheRate
{
    /// <summary>
    /// Gets or sets the observation date of the rate, formatted as invariant <c>yyyy-MM-dd</c> text.
    /// </summary>
    /// <value>The observation date as invariant ISO text.</value>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observed rate, formatted as an invariant decimal string so its precision and scale round-trip
    /// exactly.
    /// </summary>
    /// <value>The observed rate as invariant decimal text.</value>
    public string Rate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant at which the rate was written to the cache, formatted as invariant round-trip (
    /// <c>"O"</c>) text.
    /// </summary>
    /// <value>The caching instant as invariant round-trip text.</value>
    public string CachedAtUtc { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant the upstream data backing the rate was originally fetched, formatted as invariant
    /// round-trip (<c>"O"</c>) text, or <see langword="null" /> when the source did not supply it.
    /// </summary>
    /// <value>
    /// The upstream fetch instant as invariant round-trip text, or <see langword="null" /> when not tracked.
    /// </value>
    /// <remarks>
    /// Omitted from the serialized JSON when <see langword="null" /> so legacy-shaped blobs stay minimal; a blob
    /// written before this field existed therefore reads back as <see langword="null" />.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ObservedAtUtc { get; set; }
}

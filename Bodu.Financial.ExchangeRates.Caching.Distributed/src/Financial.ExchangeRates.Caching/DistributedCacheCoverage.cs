// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The JSON serialization shape of one recorded coverage window, recording that the inclusive range
/// <see cref="Start" />..<see cref="End" /> was fetched from the provider at <see cref="FetchedAtUtc" />.
/// </summary>
/// <remarks>
/// The fetch instant drives expiry of the coverage the same way a cached row's caching instant drives expiry of the
/// rate. All three fields are persisted as invariant text — the two dates as <c>yyyy-MM-dd</c> and the fetch instant in
/// round-trip (<c>"O"</c>) form — so the window round-trips losslessly.
/// </remarks>
internal sealed class DistributedCacheCoverage
{
    /// <summary>
    /// Gets or sets the inclusive first date of the fetched range, formatted as invariant <c>yyyy-MM-dd</c> text.
    /// </summary>
    /// <value>The first date covered by the window, as invariant ISO text.</value>
    public string Start { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inclusive last date of the fetched range, formatted as invariant <c>yyyy-MM-dd</c> text.
    /// </summary>
    /// <value>The last date covered by the window, as invariant ISO text.</value>
    public string End { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant at which the range was fetched and recorded as covered, formatted as invariant
    /// round-trip (<c>"O"</c>) text.
    /// </summary>
    /// <value>The instant the window was recorded, as invariant round-trip text.</value>
    public string FetchedAtUtc { get; set; } = string.Empty;
}
